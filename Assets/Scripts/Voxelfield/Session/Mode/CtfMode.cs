using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util;
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
        [SerializeField] private byte m_HealthPackBonus = 75;

        // private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];
        private (PickUpBehavior[], FlagBehavior[][]) m_Behaviors;
        private VoxelMapNameProperty m_LastMapName;

        public override void Initialize() => m_LastMapName = new VoxelMapNameProperty();

        private (PickUpBehavior[], FlagBehavior[][]) GetBehaviors()
        {
            StringProperty mapName = MapManager.Singleton.Map.name;
            if (m_LastMapName == mapName) return m_Behaviors;
            m_LastMapName.SetTo(mapName);
            return m_Behaviors = (MapManager.Singleton.Models.Values
                                            .Where(model => model is PickUpBehavior)
                                            .Cast<PickUpBehavior>()
                                            .OrderBy(model => model.Position.GetHashCode())
                                            .ToArray(),
                                  MapManager.Singleton.Models.Values
                                            .Where(model => model is FlagBehavior)
                                            .GroupBy(model => model.Container.Require<TeamProperty>().Value)
                                            .OrderBy(group => group.Key)
                                            .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray());
        }

        public override void BeginModify(in SessionContext context)
        {
            base.BeginModify(in context);
            context.sessionContainer.Require<CtfComponent>().pickupFlags.Value = uint.MaxValue;
            context.sessionContainer.Require<DualScoresArray>().Zero();
        }

        public override void Render(in SessionContext context)
        {
            base.Render(context);

            (PickUpBehavior[] pickUpBehaviors, FlagBehavior[][] flagBehaviors) = GetBehaviors();
            var ctf = context.sessionContainer.Require<CtfComponent>();

            TeamFlagArray flags = ctf.teamFlags;
            for (var flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
                flagBehaviors[flagTeam][flagId].Render(context, flags[flagTeam][flagId]);

            for (var i = 0; i < pickUpBehaviors.Length; i++)
                pickUpBehaviors[i].Render(FlagUtil.HasFlag(ctf.pickupFlags, i), context.timeUs);
        }

        public override void Modify(in SessionContext context)
        {
            base.Modify(context);

            var ctf = context.sessionContainer.Require<CtfComponent>();
            (PickUpBehavior[] pickUpBehaviors, FlagBehavior[][] flagBehaviors) = GetBehaviors();

            for (byte flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
            {
                FlagComponent flag = ctf.teamFlags[flagTeam][flagId];
                HandlePlayersNearFlag(context, flag, flagTeam, flagId, ctf, context.sessionContainer.Require<DualScoresArray>());
                if (flag.captureElapsedTimeUs.WithValue) flag.captureElapsedTimeUs.Add(context.durationUs);
            }

            for (var i = 0; i < pickUpBehaviors.Length; i++)
            {
                TimeUsProperty cooldown = ctf.pickupCoolDowns[i];
                if (cooldown.Subtract(context.durationUs, true))
                    FlagUtil.SetFlag(ref ctf.pickupFlags.DirectValue, i);
                if (cooldown.WithValue) continue;

                PickUpBehavior pickUpBehavior = pickUpBehaviors[i];
                int count = Physics.OverlapSphereNonAlloc(pickUpBehavior.Position, 1.25f, m_CachedColliders, m_PlayerMask);
                for (var j = 0; j < count; j++)
                {
                    if (!m_CachedColliders[j].TryGetComponent(out PlayerTrigger playerTrigger)) continue;

                    /* Picked up */
                    Container player = context.GetModifyingPlayer(playerTrigger.PlayerId);
                    var success = true;
                    switch (pickUpBehavior.T)
                    {
                        case PickUpBehavior.Type.Health:
                            if (player.Health() >= Config.Active.respawnHealth) success = false;
                            else player.Health().Increment(m_HealthPackBonus, Config.Active.respawnHealth);
                            break;
                        case PickUpBehavior.Type.Ammo:
                            PlayerItemManagerModiferBehavior.RefillAllAmmo(player.Require<InventoryComponent>());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (success)
                    {
                        FlagUtil.UnsetFlag(ref ctf.pickupFlags.DirectValue, i);
                        cooldown.Value = 10_000_000u;
                        break;
                    }
                }
            }

            var pickupCount = 0;
            var entities = context.sessionContainer.Require<EntityArray>();
            for (var i = 0; i < entities.Length; i++)
            {
                EntityContainer entity = entities[i];
                var throwable = entity.Require<ThrowableComponent>();
                if (throwable.flags.IsFloating && throwable.flags.IsPersistent)
                    pickupCount++;
            }
            if (pickupCount == 0)
            {
                byte pickupItemId = m_PickupItemIds[Random.Range(0, m_PickupItemIds.Length)].id;
                CreateItemEntity(context, DeathmatchMode.GetRandomPosition(), pickupItemId,
                                 flags: ThrowableFlags.Floating | ThrowableFlags.Persistent);
            }
        }

        public override bool ShowScoreboard(in SessionContext context)
            => context.sessionContainer.Require<DualScoresArray>().Any(score => score >= 3);

        public override void ModifyPlayer(in SessionContext context)
        {
            base.ModifyPlayer(context);

            context.player.Require<FrozenProperty>().Value = ShowScoreboard(context);

            if (context.player.Health().IsAlive) return;

            var wantedItems = context.commands.Require<WantedItemIdArray>();
            var inventory = context.player.Require<InventoryComponent>();
            for (var i = 1; i < wantedItems.Length; i++)
            {
                ByteProperty wantedId = wantedItems[i];
                if (inventory[i].id != wantedId) PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, wantedId.AsNullable, i);
            }
        }

        protected override void HandleAutoRespawn(in SessionContext context, HealthProperty health)
        {
            if (health.IsAlive || context.player.Without(out RespawnTimerProperty respawn)) return;

            if (respawn.Subtract(context.durationUs) && context.commands.Require<InputFlagProperty>().GetInput(PlayerInput.Respawn))
                SpawnPlayer(context);
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
                return AdjustSpawn(teamSpawns[spawnIndex].Key);
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

        private void HandlePlayersNearFlag(in SessionContext context, FlagComponent flag, byte flagTeam, int flagId, CtfComponent ctf, DualScoresArray scores)
        {
            (_, FlagBehavior[][] flagBehaviors) = GetBehaviors();
            int count = Physics.OverlapSphereNonAlloc(flagBehaviors[flagTeam][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
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
                if (enemyTaking.Health().IsInactiveOrDead || flag.captureElapsedTimeUs < TakeFlagDurationUs) enemyTaking = null;
            }
            else enemyTaking = enemyTakingIn;
            if (enemyTaking is null)
            {
                // Return flag if capturing player has left early when taking or has disconnected or died
                flag.Clear();
            }
        }

        private static void TryReturnCapturedFlag(byte playerTeam, DualScoresArray scores, CtfComponent ctf, PlayerTrigger player)
        {
            for (var flagTeam = 0; flagTeam < ctf.teamFlags.Length; flagTeam++)
            {
                if (flagTeam == playerTeam) continue; // Continue if friendly flag
                FlagArray enemyFlags = ctf.teamFlags[flagTeam];
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
            if (begin)
            {
                player.Require<TeamProperty>().Value = (byte) (context.playerId % 2);
                player.ZeroIfWith<StatsComponent>();
            }
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