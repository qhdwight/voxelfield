using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels.Map;
using Random = UnityEngine.Random;

namespace Voxelfield.Session.Mode
{
    using QueuedTeamSpawns = IReadOnlyList<Queue<KeyValuePair<Position3Int, Container>>>;

    [CreateAssetMenu(fileName = "Showdown", menuName = "Session/Mode/Showdown", order = 0)]
    public class ShowdownMode : DeathmatchModeBase, IModeWithBuying
    {
        // public const uint BuyTimeUs = 15_000_000u, FightTimeUs = 300_000_000u;
        // public const uint BuyTimeUs = 60_000_000u, FightTimeUs = 300_000_000u;
        public const uint BuyTimeUs = 2_000_000u, FightTimeUs = 5_000_000u;
        public const uint SecureTimeUs = 5_000_000;

        private const int TeamCount = 5, PlayersPerTeam = 3, TotalPlayers = TeamCount * PlayersPerTeam;

        [SerializeField] private LayerMask m_ModelMask = default;
        private CurePackageBehavior[] m_CurePackages;
        private static readonly RaycastHit[] CachedHits = new RaycastHit[1];

        public override void Modify(in SessionContext context)
        {
            base.Modify(context);
            var stage = context.sessionContainer.Require<ShowdownSessionComponent>();
            if (stage.number.WithoutValue) // If in warmup
            {
                int playerCount = GetActivePlayerCount(context.sessionContainer);
                if (playerCount == 1)
                    // if (playerCount == TotalPlayers)
                {
                    StartFirstStage(context, stage);
                }
            }
            if (stage.number.WithValue)
            {
                if (stage.remainingUs > context.durationUs) stage.remainingUs.Value -= context.durationUs;
                else
                {
                    // Advance stage
                    stage.remainingUs.Value = FightTimeUs + BuyTimeUs;
                    stage.number.Value++;
                    SetActiveCures(stage);
                }
            }
        }

        public override void ModifyPlayer(in SessionContext context)
        {
            base.ModifyPlayer(in context);

            var stage = context.sessionContainer.Require<ShowdownSessionComponent>();
            if (stage.number.WithoutValue) return;

            bool isFightTime = stage.remainingUs < FightTimeUs;
            context.player.Require<FrozenProperty>().Value = !isFightTime;
            if (isFightTime)
            {
                if (context.tickDelta >= 1)
                {
                    var showdownPlayer = context.player.Require<ShowdownPlayerComponent>();
                    PlayerSecuring(context, showdownPlayer, stage);
                }
            }
            else
            {
                BuyingMode.HandleBuying(this, context.player, context.commands);
            }
        }

        public bool IsLookingAt<TBehavior>(Container player, out TBehavior behavior)
        {
            Ray ray = player.GetRayForPlayer();
            int count = Physics.RaycastNonAlloc(ray, CachedHits, 2.0f, m_ModelMask);
            if (CachedHits.TryClosest(count, out RaycastHit hit) && hit.collider.TryGetComponent(out behavior))
                return true;
            behavior = default;
            return false;
        }

        private void PlayerSecuring(in SessionContext sessionContext, ShowdownPlayerComponent showdownPlayer, ShowdownSessionComponent stage)
        {
            var isInteracting = false;
            CurePackageComponent cure = default;
            if (sessionContext.commands.Require<InputFlagProperty>().GetInput(PlayerInput.Interact)
             && IsLookingAt(sessionContext.player, out CurePackageBehavior curePackage))
            {
                cure = stage.curePackages[curePackage.Container.Require<ByteIdProperty>()];
                if (cure.isActive.WithValueEqualTo(true)) isInteracting = true;
            }
            if (isInteracting)
            {
                showdownPlayer.elapsedSecuringUs.Add(sessionContext.durationUs);
                if (showdownPlayer.elapsedSecuringUs > SecureTimeUs)
                {
                    Secure(showdownPlayer, stage, cure);
                }
            }
            else
            {
                showdownPlayer.elapsedSecuringUs.Value = 0u;
            }
        }

        private static void Secure(ShowdownPlayerComponent showdownPlayer, ShowdownSessionComponent stage, CurePackageComponent cure)
        {
            showdownPlayer.stagesCuredFlags.Value |= (byte) (1 << stage.number);
            cure.isActive.Value = false;
        }

        protected override void HandleAutoRespawn(in SessionContext context, HealthProperty health)
        {
            if (InWarmup(context.sessionContainer)) base.HandleAutoRespawn(in context, health); // Random respawn
        }

        private static void FirstStageSpawn(in SessionContext sessionContext, QueuedTeamSpawns spawns)
        {
            Container player = sessionContext.player;
            player.Require<TeamProperty>().Value = (byte) (sessionContext.playerId % PlayersPerTeam);

            var move = player.Require<MoveComponent>();
            move.Zero();
            // TODO:refactor use rotation
            Vector3 position = spawns[player.Require<TeamProperty>()].Dequeue().Key;
            move.position.Value = position;
            player.ZeroIfWith<CameraComponent>();
            player.Require<MoneyComponent>().count.Value = ushort.MaxValue;
            if (player.With(out HealthProperty health)) health.Value = DefaultConfig.Active.respawnHealth;
            if (player.With(out InventoryComponent inventory))
            {
                PlayerItemManagerModiferBehavior.ResetEquipStatus(inventory);
                PlayerItemManagerModiferBehavior.SetAllItems(inventory, ItemId.Pickaxe, ItemId.Pistol);
            }
            player.Require<ShowdownPlayerComponent>().Zero();
        }

        private static void StartFirstStage(in SessionContext context, ShowdownSessionComponent stage)
        {
            ModelsProperty models = MapManager.Singleton.Map.models;
            QueuedTeamSpawns spawns = FindSpawns(models);
            stage.number.Value = 0;
            stage.remainingUs.Value = BuyTimeUs + FightTimeUs;
            SetActiveCures(stage);
            context.ForEachActivePlayer((in SessionContext playerModifyContext) => FirstStageSpawn(playerModifyContext, spawns));
            Debug.Log("Started first stage");
        }

        private static Queue<KeyValuePair<Position3Int, Container>>[] FindSpawns(ModelsProperty models)
            => models.Map.Where(modelPair => modelPair.Value.With<TeamProperty>())
                     .GroupBy(spawnPair => spawnPair.Value.Require<TeamProperty>().Value)
                     .OrderBy(group => group.Key)
                     .Select(teamGroup => new Queue<KeyValuePair<Position3Int, Container>>(teamGroup))
                     .ToArray();

        private static void SetActiveCures(ShowdownSessionComponent stage)
        {
            if (stage.number == 0)
            {
                for (var index = 0; index < stage.curePackages.Length; index++)
                {
                    CurePackageComponent package = stage.curePackages[index];
                    package.isActive.Value = index % 2 == 1;
                }
                return;
            }

            var choseFrom = new List<int>();
            for (var index = 0; index < stage.curePackages.Length; index++)
            {
                CurePackageComponent package = stage.curePackages[index];
                if (package.isActive) package.isActive.Value = false;
                else choseFrom.Add(index);
            }
            choseFrom.RemoveAt(Random.Range(0, choseFrom.Count));
            foreach (int index in choseFrom)
            {
                stage.curePackages[index].isActive.Set();
            }
        }

        private static bool InWarmup(Container session) => session.Require<ShowdownSessionComponent>().number.WithoutValue;

        protected override float CalculateWeaponDamage(in PlayerHitContext context)
        {
            if (!InWarmup(context.sessionContext.sessionContainer) &&
                context.hitPlayer.Require<TeamProperty>() == context.sessionContext.player.Require<TeamProperty>()) return 0.0f;

            float baseDamage = base.CalculateWeaponDamage(context);
            return CalculateDamageWithMovement(context, baseDamage);
        }

        public static float CalculateDamageWithMovement(in PlayerHitContext context, float baseDamage)
        {
            if (context.weapon is MeleeModifier) return baseDamage;
            // Nerf damage while on the run
            Container inflictingPlayer = context.sessionContext.player;
            Vector3 velocity = inflictingPlayer.Require<MoveComponent>().velocity;
            var modifierPrefab = (PlayerModifierDispatcherBehavior) context.sessionContext.session.PlayerManager.GetModifierPrefab(inflictingPlayer.Require<ByteIdProperty>());
            float ratio = 1.0f - Mathf.Clamp01(velocity.LateralMagnitude() / modifierPrefab.Movement.MaxSpeed);
            const float minimumRatio = 0.3f;
            ratio = minimumRatio + ratio * (1.0f - minimumRatio);
            return baseDamage * ratio;
        }

        public override bool AllowTeamSwitch(in SessionContext context) => InWarmup(context.sessionContainer);

        public override void Render(in SessionContext context)
        {
            base.Render(context);

            if (MapManager.Singleton.Models.Count == 0) return;
            ArrayElement<CurePackageComponent> cures = context.sessionContainer.Require<ShowdownSessionComponent>().curePackages;
            // TODO:performance
            m_CurePackages = MapManager.Singleton.Models.Values
                                       .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Cure)
                                       .Cast<CurePackageBehavior>()
                                       .ToArray();
            for (var index = 0; index < cures.Length; index++)
                m_CurePackages[index].Render(cures[index]);
        }

        // public override void SetupNewPlayer(SessionBase session, Container player)
        // {
        //     Container container = session.GetLatestSession();
        //     if (container.Require<ShowdownSessionComponent>().number == WarmupNumber)
        //     {
        //         player.Require<TeamProperty>().Value = 
        //     }
        // }

        public bool CanBuy(in SessionContext context, Container sessionLocalPlayer)
            => sessionLocalPlayer.Health().IsAlive && context.sessionContainer.Require<ShowdownSessionComponent>().number.WithValue;

        public ushort GetCost(int itemId) => throw new NotImplementedException();
    }
}