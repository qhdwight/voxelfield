using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels.Map;
using Random = UnityEngine.Random;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Capture The Flag", menuName = "Session/Mode/Capture The Flag", order = 0)]
    public class CtfMode : DeathmatchModeBase
    {
        public const byte BlueTeam = 0, RedTeam = 1;

        public const uint TakeFlagDurationUs = 2_000_000u;

        [SerializeField] private LayerMask m_PlayerMask = default;
        [SerializeField] private float m_CaptureRadius = 3.0f;
        [SerializeField] private Color m_BlueColor = new Color(0.1764705882f, 0.5098039216f, 0.8509803922f),
                                       m_RedColor = new Color(0.8196078431f, 0.2156862745f, 0.1960784314f);
        [SerializeField] private ItemModifierBase[] m_PickupItemIds = default;

        // private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];
        private FlagBehavior[][] m_FlagBehaviors;
        private VoxelMapNameProperty m_LastMapName;

        public override void Initialize() => m_LastMapName = new VoxelMapNameProperty();

        private FlagBehavior[][] GetFlagBehaviors()
        {
            StringProperty mapName = MapManager.Singleton.Map.name;
            if (m_LastMapName == mapName) return m_FlagBehaviors;
            m_LastMapName.SetTo(mapName);
            return m_FlagBehaviors = MapManager.Singleton.Models.Values
                                               .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Flag)
                                               .GroupBy(model => model.Container.Require<TeamProperty>().Value)
                                               .OrderBy(group => group.Key)
                                               .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray();
        }

        public override void BeginModify(in SessionContext context)
        {
            base.BeginModify(in context);
            context.sessionContainer.Require<DualScoresComponent>().Zero();
        }

        public override void Render(in SessionContext context)
        {
            base.Render(context);

            FlagBehavior[][] flagBehaviors = GetFlagBehaviors();
            ArrayElement<FlagArrayElement> flags = context.sessionContainer.Require<CtfComponent>().teamFlags;
            for (var flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
                flagBehaviors[flagTeam][flagId].Render(context, flags[flagTeam][flagId]);
        }

        public override void Modify(in SessionContext context)
        {
            base.Modify(context);

            var ctf = context.sessionContainer.Require<CtfComponent>();
            FlagBehavior[][] flagBehaviors = GetFlagBehaviors();
            for (byte flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
            {
                FlagComponent flag = ctf.teamFlags[flagTeam][flagId];
                HandlePlayersNearFlag(context, flag, flagTeam, flagId, ctf, context.sessionContainer.Require<DualScoresComponent>());
                if (flag.captureElapsedTimeUs.WithValue) flag.captureElapsedTimeUs.Value += context.durationUs;
            }

            var pickupCount = 0;
            var entities = context.sessionContainer.Require<EntityArrayElement>();
            for (var i = 0; i < entities.Length; i++)
            {
                EntityContainer entity = entities[i];
                var throwable = entity.Require<ThrowableComponent>();
                if (throwable.isFrozen.WithValueEqualTo(true))
                {
                    pickupCount++;
                }
            }
            if (pickupCount == 0)
            {
                byte itemId = m_PickupItemIds[Random.Range(0, m_PickupItemIds.Length)].id,
                     entityId = (byte) (itemId + 100);
                (ModifierBehaviorBase modifier, Container entity) = context.session.EntityManager.ObtainNextModifier(context.sessionContainer, entityId);
                var throwable = entity.Require<ThrowableComponent>();
                Vector3 position = DeathmatchMode.GetRandomPosition() + new Vector3 {y = 1.0f};
                throwable.position.Value = position;
                throwable.isFrozen.Value = true;
                modifier.transform.SetPositionAndRotation(position, Quaternion.identity);
                var item = entity.Require<ItemComponent>();
                if (ItemAssetLink.GetModifier(itemId) is GunModifierBase gunModifier)
                {
                    item.ammoInMag.Value = gunModifier.MagSize;
                    item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
                }
            }
        }

        public override bool ShowScoreboard(in SessionContext context)
            => context.sessionContainer.Require<DualScoresComponent>().Any(score => score >= 3);

        public override void ModifyPlayer(in SessionContext context)
        {
            base.ModifyPlayer(context);

            context.player.Require<FrozenProperty>().Value = ShowScoreboard(context);

            if (context.player.Require<HealthProperty>().IsAlive) return;

            var wantedItems = context.commands.Require<WantedItemIdsComponent>();
            var inventory = context.player.Require<InventoryComponent>();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 1; i < wantedItems.Length; i++)
            {
                ByteProperty wantedId = wantedItems[i];
                if (inventory[i].id != wantedId) PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, wantedId.AsNullable, i);
            }
        }

        protected override void HandleAutoRespawn(in SessionContext context, HealthProperty health)
        {
            if (health.IsAlive || context.player.Without(out RespawnTimerProperty respawn)) return;

            if (respawn.Value > context.durationUs) respawn.Value -= context.durationUs;
            else
            {
                respawn.Value = 0u;
                if (context.commands.Require<InputFlagProperty>().GetInput(PlayerInput.Respawn))
                    SpawnPlayer(context);
            }
        }

        protected override Vector3 GetSpawnPosition(in SessionContext context)
        {
            try
            {
                KeyValuePair<Position3Int, Container>[][] spawns = MapManager.Singleton.Map.models.Map.Where(pair => pair.Value.With(out ModelIdProperty modelId)
                                                                                                                  && modelId == ModelsProperty.Spawn
                                                                                                                  && pair.Value.With<TeamProperty>())
                                                                             .GroupBy(spawnPair => spawnPair.Value.Require<TeamProperty>().Value)
                                                                             .OrderBy(spawnGroup => spawnGroup.Key)
                                                                             .Select(spawnGroup => spawnGroup.ToArray())
                                                                             .ToArray();
                byte team = context.player.Require<TeamProperty>();
                KeyValuePair<Position3Int, Container>[] teamSpawns = spawns[team];
                int spawnIndex = Random.Range(0, teamSpawns.Length);
                Vector3 spawnPosition = teamSpawns[spawnIndex].Key;
                for (var _ = 0; _ < 16; _++, spawnPosition += new Vector3 {y = 3.0f})
                {
                    if (Physics.Raycast(spawnPosition, Vector3.down, out RaycastHit hit, float.PositiveInfinity))
                        return hit.point + new Vector3 {y = 0.1f};
                }
                throw new Exception("No non-obstructed spawn points");
            }
            catch (Exception)
            {
                return DeathmatchMode.GetRandomPosition();
            }
        }

        protected override float CalculateWeaponDamage(in PlayerHitContext context)
            => context.hitPlayer.Require<TeamProperty>() == context.sessionContext.player.Require<TeamProperty>()
                ? 0.0f
                : ShowdownMode.CalculateDamageWithMovement(context, base.CalculateWeaponDamage(context));

        private void HandlePlayersNearFlag(in SessionContext context, FlagComponent flag, byte flagTeam, int flagId, CtfComponent ctf, DualScoresComponent scores)
        {
            int count = Physics.OverlapSphereNonAlloc(m_FlagBehaviors[flagTeam][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
            Container enemyTakingIn = null;
            for (var i = 0; i < count; i++)
            {
                if (!m_CachedColliders[i].TryGetComponent(out PlayerTrigger playerTrigger)) continue;

                var playerIdInFlag = (byte) playerTrigger.PlayerId;
                Container player = context.GetModifyingPlayer(playerIdInFlag);
                if (player.Require<TeamProperty>() == flagTeam)
                {
                    TryReturnCapturedFlag(flagTeam, scores, ctf, playerTrigger); // Possibly returning captured enemy flag to friendly flag
                }
                else
                {
                    /* Enemy trying to capture flag */
                    if (flag.capturingPlayerId.WithoutValue)
                    {
                        // Start taking
                        flag.capturingPlayerId.Value = playerIdInFlag;
                        flag.captureElapsedTimeUs.Value = 0u;
                        enemyTakingIn = player;
                    }
                    else if (flag.capturingPlayerId == playerIdInFlag)
                    {
                        // Enemy in progress of taking
                        enemyTakingIn = player;
                    }
                }
            }
            Container enemyTaking;
            if (enemyTakingIn is null && flag.capturingPlayerId.WithValue)
            {
                // Flag is taken, but the capturing player is outside the radius of taking
                enemyTaking = context.GetModifyingPlayer(flag.capturingPlayerId);
                // Return flag if capturing player disconnects / dies or if they have not fully taken
                if (enemyTaking.Require<HealthProperty>().IsInactiveOrDead || flag.captureElapsedTimeUs < TakeFlagDurationUs) enemyTaking = null;
            }
            else enemyTaking = enemyTakingIn;
            if (enemyTaking is null)
            {
                // Return flag if capturing player has left early when taking or has disconnected or died
                flag.Clear();
            }
        }

        private static void TryReturnCapturedFlag(byte playerTeam, DualScoresComponent scores, CtfComponent ctf, PlayerTrigger player)
        {
            for (var flagTeam = 0; flagTeam < ctf.teamFlags.Length; flagTeam++)
            {
                if (flagTeam == playerTeam) continue; // Continue if friendly flag
                FlagArrayElement enemyFlags = ctf.teamFlags[flagTeam];
                // ReSharper disable once ForCanBeConvertedToForeach - Avoid allocation of getting enumerator
                for (var i = 0; i < enemyFlags.Length; i++)
                {
                    FlagComponent enemyFlag = enemyFlags[i];
                    if (enemyFlag.capturingPlayerId.WithValueEqualTo((byte) player.PlayerId)
                     && enemyFlag.captureElapsedTimeUs > TakeFlagDurationUs) // Test if friendly returning enemy flag
                    {
                        enemyFlag.Clear();
                        scores[playerTeam].Value++;
                    }
                }
            }
        }

        protected override void SpawnPlayer(in SessionContext context, bool begin = false)
        {
            Container player = context.player;
            if (begin) player.Require<TeamProperty>().Value = (byte) (context.playerId % 2);
            if (begin) player.ZeroIfWith<StatsComponent>();
            player.Require<ByteIdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = begin ? (byte) 0 : DefaultConfig.Active.respawnHealth;
            player.ZeroIfWith<RespawnTimerProperty>();
            if (player.With(out InventoryComponent inventory))
            {
                PlayerItemManagerModiferBehavior.ResetEquipStatus(inventory);
                if (begin) PlayerItemManagerModiferBehavior.SetAllItems(inventory, ItemId.Pickaxe);
                else PlayerItemManagerModiferBehavior.RefillAllAmmo(inventory);
            }
            if (player.With(out MoveComponent move))
            {
                if (begin) move.Clear();
                else
                {
                    move.Zero();
                    move.position.Value = GetSpawnPosition(context);
                }
            }
        }

        public override Color GetTeamColor(byte? teamId) => teamId is byte team
            ? team == BlueTeam ? m_BlueColor : m_RedColor
            : Color.white;
    }
}