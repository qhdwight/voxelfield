using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
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
    using TeamSpawns = IReadOnlyList<Queue<(Position3Int, Container)>>;

    [CreateAssetMenu(fileName = "Showdown", menuName = "Session/Mode/Showdown", order = 0)]
    public class ShowdownMode : DeathmatchMode
    {
        // public const uint BuyTimeUs = 15_000_000u, FightTimeUs = 300_000_000u;
        // public const uint BuyTimeUs = 60_000_000u, FightTimeUs = 300_000_000u;
        public const uint BuyTimeUs = 10_000_000u, FightTimeUs = 300_000_000u;

        private const int TeamCount = 5, PlayersPerTeam = 3, TotalPlayers = TeamCount * PlayersPerTeam;

        [SerializeField] private CurePackageVisuals m_CurePackageVisualPrefab = default;

        private CurePackageVisuals[] m_CurePackageVisuals;

        public override void Modify(SessionBase session, Container container, uint durationUs)
        {
            base.Modify(session, container, durationUs);
            var stage = container.Require<ShowdownSessionComponent>();
            if (stage.number.WithoutValue) // If in warmup
            {
                int playerCount = GetPlayerCount(container);
                if (playerCount == 1)
                    // if (playerCount == TotalPlayers)
                {
                    StartFirstStage(session, container, stage);
                }
            }
            if (stage.number.WithValue)
            {
                if (stage.remainingUs > durationUs) stage.remainingUs.Value -= durationUs;
                else stage.remainingUs.Value = 0u;
            }
        }

        public override void ModifyPlayer(SessionBase session, Container container, Container player, Container commands, uint durationUs)
        {
            base.ModifyPlayer(session, container, player, commands, durationUs);

            var stage = container.Require<ShowdownSessionComponent>();
            if (stage.number.WithoutValue) return;

            bool isBuyTime = stage.remainingUs > FightTimeUs;
            player.Require<FrozenProperty>().Value = isBuyTime;
            if (isBuyTime)
            {
                ByteProperty wantedBuyItemId = player.Require<MoneyComponent>().wantedBuyItemId;
                if (wantedBuyItemId.WithValue)
                {
                    UShortProperty money = player.Require<MoneyComponent>().count;
                    Debug.Log($"Trying to buy requested item: {wantedBuyItemId.Value}");
                    ushort cost = GetCost(wantedBuyItemId);
                    if (cost < money)
                    {
                        var inventory = player.Require<InventoryComponent>();
                        PlayerItemManagerModiferBehavior.AddItem(inventory, wantedBuyItemId);
                        money.Value -= cost;
                    }
                }
            }
        }

        private static ushort GetCost(byte itemId)
        {
            switch (itemId)
            {
                case ItemId.Rifle:
                    return 2000;
                case ItemId.Shotgun:
                    return 1300;
                case ItemId.Sniper:
                    return 5000;
                case ItemId.Deagle:
                    return 700;
                case ItemId.Grenade:
                    return 150;
                case ItemId.Molotov:
                    return 400;
                case ItemId.C4:
                    return 600;
            }
            throw new ArgumentException("Can't buy this item id");
        }

        protected override void HandleRespawn(SessionBase session, Container container, Container player, HealthProperty health, uint durationUs)
        {
            if (InWarmup(container)) base.HandleRespawn(session, container, player, health, durationUs); // Random respawn
        }

        private static void FirstStageSpawn(Container session, int playerId, Container player, TeamSpawns spawns)
        {
            player.Require<TeamProperty>().Value = (byte) (playerId % PlayersPerTeam);

            var move = player.Require<MoveComponent>();
            move.Zero();
            // TODO:refactor use rotation
            (Vector3 position, Container _) = spawns[player.Require<TeamProperty>()].Dequeue();
            move.position.Value = position;
            player.ZeroIfWith<CameraComponent>();
            player.Require<MoneyComponent>().count.Value = ushort.MaxValue;
            if (player.With(out HealthProperty health)) health.Value = 100;
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.AddItem(inventory, ItemId.Shovel);
                PlayerItemManagerModiferBehavior.AddItem(inventory, ItemId.Pistol);
            }
        }

        private static void StartFirstStage(SessionBase session, Container sessionContainer, ShowdownSessionComponent stage)
        {
            ModelsProperty models = MapManager.Singleton.Map.models;
            TeamSpawns spawns = models.Where(modelTuple => modelTuple.Item2.With<TeamProperty>())
                                      .GroupBy(spawnTuple => spawnTuple.Item2.Require<TeamProperty>().Value)
                                      .Select(teamGroup => new Queue<(Position3Int, Container)>(teamGroup))
                                      .ToArray();
            stage.number.Value = 0;
            stage.remainingUs.Value = BuyTimeUs + FightTimeUs;
            Vector3[] curePositions = models.Where(modelTuple => modelTuple.Item2.Require<ModelIdProperty>() == ModelsProperty.Cure)
                                            .OrderBy(modelTuple => modelTuple.Item2.Require<IdProperty>().Value)
                                            .Select(cureTuple => (Vector3) cureTuple.Item1)
                                            .ToArray();
            for (var index = 0; index < stage.curePackages.Length; index++)
            {
                CurePackageComponent package = stage.curePackages[index];
                package.isActive.Value = true;
                package.position.Value = curePositions[index];
            }
            ForEachActivePlayer(session, sessionContainer, (playerId, player) => FirstStageSpawn(sessionContainer, playerId, player, spawns));
            Debug.Log("Started first stage");
        }

        // TODO:performance LINQ creates too much garbage?
        private static int GetPlayerCount(Container session)
            => session.Require<PlayerContainerArrayElement>().Count(player => player.Require<HealthProperty>().WithValue);

        private static bool InWarmup(Container session) => session.Require<ShowdownSessionComponent>().number.WithoutValue;

        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer,
                                                       PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            if (!InWarmup(session.GetLatestSession()) && hitPlayer.Require<TeamProperty>() == inflictingPlayer.Require<TeamProperty>()) return 0.0f;

            // Nerf damage while on the run
            float baseDamage = base.CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, hit);
            if (weapon is MeleeModifier) return baseDamage;
            Vector3 velocity = inflictingPlayer.Require<MoveComponent>().velocity;
            var modifierPrefab = (PlayerModifierDispatcherBehavior) session.PlayerManager.GetModifierPrefab(inflictingPlayer.Require<IdProperty>());
            float ratio = 1.0f - Mathf.Clamp01(velocity.LateralMagnitude() / modifierPrefab.Movement.MaxSpeed);
            const float minimumRatio = 0.3f;
            ratio = minimumRatio + ratio * (1.0f - minimumRatio);
            return baseDamage * ratio;
        }

        public override bool AllowTeamSwap(Container container, Container player) => InWarmup(container);

        public override void Render(Container container)
        {
            ArrayElement<CurePackageComponent> cures = container.Require<ShowdownSessionComponent>().curePackages;
            if (m_CurePackageVisuals == null)
                m_CurePackageVisuals = Enumerable.Range(0, 9)
                                                 .Select(_ => Instantiate(m_CurePackageVisualPrefab))
                                                 .ToArray();
            for (var index = 0; index < cures.Length; index++)
                m_CurePackageVisuals[index].Render(cures[index]);
        }

        private void OnEnable() => m_CurePackageVisuals = null;

        public override void Dispose()
        {
            if (m_CurePackageVisuals != null)
                foreach (CurePackageVisuals visual in m_CurePackageVisuals)
                    Destroy(visual.gameObject);
            m_CurePackageVisuals = null;
        }

        // public override void SetupNewPlayer(SessionBase session, Container player)
        // {
        //     Container container = session.GetLatestSession();
        //     if (container.Require<ShowdownSessionComponent>().number == WarmupNumber)
        //     {
        //         player.Require<TeamProperty>().Value = 
        //     }
        // }
    }
}