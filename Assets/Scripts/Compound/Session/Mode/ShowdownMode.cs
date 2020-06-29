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

namespace Compound.Session.Mode
{
    using TeamSpawns = IReadOnlyList<Queue<(Position3Int, Container)>>;

    [CreateAssetMenu(fileName = "Showdown", menuName = "Session/Mode/Showdown", order = 0)]
    public class ShowdownMode : DeathmatchMode
    {
        private const int TeamCount = 5, PlayersPerTeam = 3, TotalPlayers = TeamCount * PlayersPerTeam;

        public const uint BuyTimeUs = 15_000_000u, FightTimeUs = 300_000_000u;

        public override void Modify(SessionBase session, Container container, uint durationUs)
        {
            base.Modify(session, container, durationUs);
            var stage = container.Require<ShowdownSessionComponent>();
            if (stage.number.WithoutValue)
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
                ForEachActivePlayer(session, container, (playerId, player) => player.Require<FrozenProperty>().Value = stage.remainingUs > FightTimeUs);
            }
        }

        protected override void HandleRespawn(SessionBase session, Container container, Container player, HealthProperty health, uint durationUs)
        {
            if (InWarmup(container)) base.HandleRespawn(session, container, player, health, durationUs);
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
            player.Require<MoneyProperty>().Value = 800;
            if (player.With(out HealthProperty health)) health.Value = 100;
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Shovel, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pistol, 2);
            }
        }

        private static void StartFirstStage(SessionBase session, Container sessionContainer, ShowdownSessionComponent stage)
        {
            // TeamSpawns spawns = MapManager.Singleton.Map.Models
            //                               .Where(pair => pair.Value.spawnTeam.HasValue)
            //                               .GroupBy(pair => pair.Value.spawnTeam.Value)
            //                               .Select(group => new Queue<(Position3Int, Quaternion)>(group.Select(pair => (pair.Key, pair.Value.rotation))))
            //                               .ToArray();
            TeamSpawns spawns = MapManager.Singleton.Map.models.GroupBy(spawnTuple => spawnTuple.Item2.Require<TeamProperty>().Value)
                                          .Select(teamGroup => new Queue<(Position3Int, Container)>(teamGroup))
                                          .ToArray();
            stage.number.Value = 0;
            stage.remainingUs.Value = BuyTimeUs + FightTimeUs;
            ForEachActivePlayer(session, sessionContainer, (playerId, player) => FirstStageSpawn(sessionContainer, playerId, player, spawns));
        }

        // TODO:performance LINQ creates too much garbage?
        private static int GetPlayerCount(Container session)
            => session.Require<PlayerContainerArrayElement>().Count(player => player.Require<HealthProperty>().WithValue);

        private static bool InWarmup(Container session) => session.Require<ShowdownSessionComponent>().number.WithoutValue;

        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer,
                                                       PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            if (!InWarmup(session.GetLatestSession()) && hitPlayer.Require<TeamProperty>() == inflictingPlayer.Require<TeamProperty>()) return 0.0f;

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