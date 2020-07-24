using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

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

        // private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];
        private FlagBehavior[][] m_FlagBehaviors;
        private VoxelMapNameProperty m_LastMapName;

        public override void Initialize() => m_LastMapName = new VoxelMapNameProperty();

        private FlagBehavior[][] GetFlagBehaviors(StringProperty mapName)
        {
            if (m_LastMapName == mapName) return m_FlagBehaviors;
            m_LastMapName.SetTo(mapName);
            return m_FlagBehaviors = MapManager.Singleton.Models.Values
                                               .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Flag)
                                               .GroupBy(model => model.Container.Require<TeamProperty>().Value)
                                               .OrderBy(group => group.Key)
                                               .Select(group => group.Cast<FlagBehavior>().ToArray()).ToArray();
        }

        public override void BeginModify(in ModifyContext context)
        {
            base.BeginModify(in context);
            var ctf = context.sessionContainer.Require<CtfComponent>();
            context.sessionContainer.Require<DualScoresComponent>().Zero();
            ctf.teamFlags.Clear();
            ForEachActivePlayer(context, (in ModifyContext playerModifyContext) =>
            {
                playerModifyContext.player.Require<HealthProperty>().Value = 0;
                playerModifyContext.player.Require<MoveComponent>().Clear();
                playerModifyContext.player.Require<InventoryComponent>().Zero();
            });
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            base.Render(session, sessionContainer);

            FlagBehavior[][] flagBehaviors = GetFlagBehaviors(sessionContainer.Require<VoxelMapNameProperty>());
            ArrayElement<FlagArrayElement> flags = sessionContainer.Require<CtfComponent>().teamFlags;
            for (var flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
                flagBehaviors[flagTeam][flagId].Render(session, sessionContainer, flags[flagTeam][flagId]);
        }

        public override void Modify(in ModifyContext context)
        {
            base.Modify(context);

            var ctf = context.sessionContainer.Require<CtfComponent>();
            FlagBehavior[][] flagBehaviors = GetFlagBehaviors(context.sessionContainer.Require<VoxelMapNameProperty>());
            for (byte flagTeam = 0; flagTeam < flagBehaviors.Length; flagTeam++)
            for (var flagId = 0; flagId < flagBehaviors[flagTeam].Length; flagId++)
            {
                FlagComponent flag = ctf.teamFlags[flagTeam][flagId];
                HandlePlayersNearFlag(context.session, flag, flagTeam, flagId, ctf, context.sessionContainer.Require<DualScoresComponent>());
                if (flag.captureElapsedTimeUs.WithValue) flag.captureElapsedTimeUs.Value += context.durationUs;
            }
        }

        public override void ModifyPlayer(in ModifyContext context)
        {
            base.ModifyPlayer(in context);

            if (context.player.Require<HealthProperty>().IsAlive) return;

            var wantedItem = context.commands.Require<WantedItemComponent>();
            if (wantedItem.id.WithValue && wantedItem.index.WithValue)
            {
                PlayerItemManagerModiferBehavior.SetItemAtIndex(context.player.Require<InventoryComponent>(), wantedItem.id, wantedItem.index);
            }
        }

        protected override void HandleAutoRespawn(in ModifyContext context, HealthProperty health)
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

        protected override Vector3 GetSpawnPosition(in ModifyContext context)
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
            return DeathmatchMode.GetRandomSpawn();
        }

        protected override float CalculateWeaponDamage(in PlayerHitContext context)
            => context.hitPlayer.Require<TeamProperty>() == context.modifyContext.player.Require<TeamProperty>()
                ? 0.0f
                : ShowdownMode.CalculateDamageWithMovement(context, base.CalculateWeaponDamage(context));

        private void HandlePlayersNearFlag(SessionBase session, FlagComponent flag, byte flagTeam, int flagId, CtfComponent ctf, DualScoresComponent scores)
        {
            int count = Physics.OverlapSphereNonAlloc(m_FlagBehaviors[flagTeam][flagId].transform.position, m_CaptureRadius, m_CachedColliders, m_PlayerMask);
            Container enemyTakingIn = null;
            for (var i = 0; i < count; i++)
            {
                if (!m_CachedColliders[i].TryGetComponent(out PlayerTrigger playerTrigger)) continue;

                var playerIdInFlag = (byte) playerTrigger.PlayerId;
                Container player = session.GetModifyingPayerFromId(playerIdInFlag);
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
                enemyTaking = session.GetModifyingPayerFromId(flag.capturingPlayerId);
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

        protected override void SpawnPlayer(in ModifyContext context)
        {
            // TODO:refactor zeroing
            Container player = context.player;
            player.ZeroIfWith<FrozenProperty>();
            player.Require<IdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = ConfigManagerBase.Active.respawnHealth;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pickaxe, 1);
                PlayerItemManagerModiferBehavior.RefillAllAmmo(inventory);
            }
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = GetSpawnPosition(context);
            }
        }

        public override void EndModify(in ModifyContext context)
        {
            context.sessionContainer.Require<CtfComponent>().Clear();
            context.sessionContainer.Require<DualScoresComponent>().Clear();
        }

        public override void SetupNewPlayer(in ModifyContext context)
        {
            Container player = context.player;
            player.Require<TeamProperty>().Value = (byte) (context.playerId % 2);
            InventoryComponent inventory = player.Require<InventoryComponent>().Zero();
            PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pickaxe, 1);
            player.ZeroIfWith<FrozenProperty>();
            player.Require<IdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = 0;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
        }

        public Color GetTeamColor(Container container) => GetTeamColor(container.Require<TeamProperty>());

        public override StringBuilder BuildUsername(StringBuilder builder, Container player)
        {
            string hex = GetHexColor(GetTeamColor(player.Require<TeamProperty>()));
            return builder.Append("<color=#").Append(hex).Append(">").AppendPropertyValue(player.Require<UsernameProperty>()).Append("</color>");
        }

        public override Color GetTeamColor(int teamId) => teamId == BlueTeam ? m_BlueColor : m_RedColor;
    }
}