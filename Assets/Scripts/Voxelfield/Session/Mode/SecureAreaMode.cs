using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Secure Area", menuName = "Session/Mode/Secure Area", order = 0)]
    public class SecureAreaMode : DeathmatchMode, IModeWithBuying
    {
        public const byte BlueTeam = 0, RedTeam = 1;

        private SiteBehavior[] m_SiteBehaviors;
        private VoxelMapNameProperty m_LastMapName;
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];

        [SerializeField] private Color m_BlueColor = new Color(0.1764705882f, 0.5098039216f, 0.8509803922f),
                                       m_RedColor = new Color(0.8196078431f, 0.2156862745f, 0.1960784314f);
        [SerializeField] private LayerMask m_PlayerTriggerMask = default;
        [SerializeField] private uint m_RoundEndDurationUs = default, m_RoundDurationUs = default, m_BuyDurationUs = default, m_SecureDurationUs = default;
        [SerializeField] private byte m_Players = default;
        [SerializeField] private ushort m_RoundWinMoney = 3000, m_RoundLoseMoney = 2000, m_KillMoney = 800;
        [SerializeField] private byte m_MaxRounds = 10;
        [SerializeField] private ushort[] m_ItemPrices = default;

        public uint SecureDurationUs => m_SecureDurationUs;
        public uint RoundDurationUs => m_RoundDurationUs;
        public uint BuyDurationUs => m_BuyDurationUs;
        public uint RoundEndDurationUs => m_RoundEndDurationUs;
        public ushort[] ItemPrices => m_ItemPrices;

        public override void Clear() => m_LastMapName = new VoxelMapNameProperty();

        private SiteBehavior[] GetSiteBehaviors(StringProperty mapName)
        {
            if (m_LastMapName == mapName) return m_SiteBehaviors;
            m_LastMapName.SetTo(mapName);
            return m_SiteBehaviors = MapManager.Singleton.Models.Values
                                               .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Site)
                                               .Cast<SiteBehavior>().ToArray();
        }

        public override void BeginModify(SessionBase session, Container sessionContainer)
        {
            base.BeginModify(session, sessionContainer);
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            secureArea.roundTime.Clear();
            secureArea.lastWinningTeam.Clear();
            sessionContainer.Require<DualScoresComponent>().Clear();
            secureArea.sites.Clear();
        }

        public override void EndModify(SessionBase session, Container sessionContainer)
        {
            ForEachActivePlayer(session, sessionContainer, (playerId, player) => player.Require<FrozenProperty>().Value = false);
        }

        protected override void HandleRespawn(SessionBase session, Container container, int playerId, Container player, HealthProperty health, uint durationUs)
        {
            if (container.Require<SecureAreaComponent>().roundTime.WithoutValue)
                base.HandleRespawn(session, container, playerId, player, health, durationUs);
        }

        protected override void KillPlayer(Container player, Container killer)
        {
            base.KillPlayer(player, killer);

            if (player.Require<TeamProperty>() != killer.Require<TeamProperty>())
            {
                UShortProperty money = killer.Require<MoneyComponent>().count;
                money.Value += m_KillMoney;
                if (money.Value > 7000) money.Value = 7000;
            }
        }

        public override void Modify(SessionBase session, Container sessionContainer, uint durationUs)
        {
            base.Modify(session, sessionContainer, durationUs);

            if (session.IsLoading) return;

            BoolProperty restartMode = Extensions.GetConfig().restartMode;
            if (restartMode)
            {
                BeginModify(session, sessionContainer);
                restartMode.Value = false;
            }

            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            int activePlayerCount = GetActivePlayerCount(sessionContainer);

            SiteBehavior[] siteBehaviors = GetSiteBehaviors(sessionContainer.Require<VoxelMapNameProperty>());
            if (secureArea.roundTime.WithValue)
            {
                bool canAdvance = true,
                     redJustSecured = false,
                     isBuyTime = secureArea.roundTime > m_RoundEndDurationUs + m_RoundDurationUs;

                var players = sessionContainer.Require<PlayerContainerArrayElement>();
                int redAlive = 0, blueAlive = 0;
                foreach (Container player in players)
                {
                    if (player.Require<HealthProperty>().IsActiveAndAlive)
                    {
                        byte team = player.Require<TeamProperty>();
                        if (team == RedTeam) redAlive++;
                        else if (team == BlueTeam) blueAlive++;
                    }
                }

                bool inRegularFightTime = secureArea.roundTime > m_RoundEndDurationUs,
                     inRealizedFightTime = !isBuyTime && (inRegularFightTime || secureArea.RedInside(out SiteComponent _)),
                     endedWithKills = false;
                if (inRealizedFightTime)
                {
                    if (inRegularFightTime && activePlayerCount > 1)
                    {
                        if (redAlive == 0)
                        {
                            sessionContainer.Require<DualScoresComponent>()[BlueTeam].Value++;
                            secureArea.lastWinningTeam.Value = BlueTeam;
                        }
                        if (blueAlive == 0)
                        {
                            sessionContainer.Require<DualScoresComponent>()[RedTeam].Value++;
                            secureArea.lastWinningTeam.Value = RedTeam;
                        }
                        if (redAlive == 0 || blueAlive == 0)
                        {
                            secureArea.roundTime.Value = m_RoundEndDurationUs;
                            endedWithKills = true;
                        }
                    }

                    for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
                    {
                        SiteBehavior siteBehavior = siteBehaviors[siteIndex];
                        Transform siteTransform = siteBehavior.transform;
                        SiteComponent site = secureArea.sites[siteIndex];
                        Vector3 bounds = siteBehavior.Container.Require<ExtentsProperty>();
                        int playersInsideCount = Physics.OverlapBoxNonAlloc(siteTransform.position, bounds / 2, m_CachedColliders, siteTransform.rotation, m_PlayerTriggerMask);
                        bool isRedInside = false, isBlueInside = false;
                        for (var i = 0; i < playersInsideCount; i++)
                        {
                            Collider collider = m_CachedColliders[i];
                            if (collider.TryGetComponent(out PlayerTrigger playerTrigger))
                            {
                                Container player = session.GetModifyingPayerFromId(playerTrigger.PlayerId);
                                if (player.Require<HealthProperty>().IsInactiveOrDead) continue;

                                byte team = player.Require<TeamProperty>();
                                if (team == RedTeam) isRedInside = true;
                                else if (team == BlueTeam) isBlueInside = true;
                            }
                        }
                        site.isRedInside.Value = isRedInside;
                        site.isBlueInside.Value = isBlueInside;
                        if (isRedInside && !isBlueInside)
                        {
                            // Red securing with no opposition
                            if (site.timeUs > durationUs) site.timeUs.Value -= durationUs;
                            else if (secureArea.roundTime >= m_RoundEndDurationUs)
                            {
                                // Round ended, site was secured by red
                                site.timeUs.Value = 0u;
                                secureArea.roundTime.Value = m_RoundEndDurationUs;
                                sessionContainer.Require<DualScoresComponent>()[RedTeam].Value++;
                                secureArea.lastWinningTeam.Value = RedTeam;
                            }
                            canAdvance = redJustSecured = site.timeUs == 0u;
                        }
                        if (isRedInside && isBlueInside) canAdvance = false; // Both in site
                    }
                }

                if (canAdvance)
                {
                    if (secureArea.roundTime > durationUs)
                    {
                        if (!redJustSecured && !endedWithKills && secureArea.roundTime >= m_RoundEndDurationUs && secureArea.roundTime - durationUs < m_RoundEndDurationUs)
                        {
                            // Round just ended without contesting
                            sessionContainer.Require<DualScoresComponent>()[BlueTeam].Value++;
                        }
                        secureArea.roundTime.Value -= durationUs;
                    }
                    else NextRound(session, sessionContainer, secureArea);
                }
                else
                {
                    if (secureArea.roundTime > m_RoundEndDurationUs && secureArea.roundTime - m_RoundEndDurationUs > durationUs) secureArea.roundTime.Value -= durationUs;
                    else secureArea.roundTime.Value = m_RoundEndDurationUs;
                }
            }
            else
            {
                // Waiting for players
                bool ForceStart()
                {
                    BoolProperty forceStart = Extensions.GetConfig().forceStart;
                    if (forceStart)
                    {
                        forceStart.Value = false;
                        return true;
                    }
                    return false;
                }
                bool start = activePlayerCount == m_Players || ForceStart();
                if (start)
                {
                    NextRound(session, sessionContainer, secureArea);
                    sessionContainer.Require<DualScoresComponent>().Zero();
                }
            }
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs, int tickDelta)
        {
            TimeUsProperty roundTime = container.Require<SecureAreaComponent>().roundTime;
            bool isBuyTime = roundTime.WithValue && roundTime > m_RoundEndDurationUs + m_RoundDurationUs;
            if (isBuyTime) BuyingMode.HandleBuying(this, player);
            player.Require<FrozenProperty>().Value = isBuyTime;

            base.ModifyPlayer(session, container, playerId, player, commands, durationUs, tickDelta);
        }

        public override void SetupNewPlayer(SessionBase session, int playerId, Container player, Container sessionContainer)
        {
            player.Require<TeamProperty>().Value = (byte) ((playerId + 1) % 2);
            base.SetupNewPlayer(session, playerId, player, sessionContainer);
        }

        protected override void SpawnPlayer(SessionBase session, Container sessionContainer, int playerId, Container player)
        {
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            if (secureArea.roundTime.WithValue)
            {
                var health = player.Require<HealthProperty>();
                if (health.IsDead)
                {
                    var inventory = player.Require<InventoryComponent>();
                    inventory.Zero();
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pickaxe, 1);
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pistol, 2);
                }

                var move = player.Require<MoveComponent>();
                move.Zero();
                move.position.Value = GetSpawnPosition(player, playerId, session, sessionContainer);
                player.ZeroIfWith<CameraComponent>();
                health.Value = 100;
                player.ZeroIfWith<HitMarkerComponent>();
                player.ZeroIfWith<DamageNotifierComponent>();
            }
            else
            {
                base.SpawnPlayer(session, sessionContainer, playerId, player);
            }
        }

        protected override Vector3 GetSpawnPosition(Container player, int playerId, SessionBase session, Container sessionContainer)
        {
            KeyValuePair<Position3Int, Container>[][] spawns = MapManager.Singleton.Map.models.Map.Where(pair => pair.Value.With(out ModelIdProperty modelId)
                                                                                                              && modelId == ModelsProperty.Spawn
                                                                                                              && pair.Value.With<TeamProperty>())
                                                                         .GroupBy(spawnPair => spawnPair.Value.Require<TeamProperty>().Value)
                                                                         .OrderBy(spawnGroup => spawnGroup.Key)
                                                                         .Select(spawnGroup => spawnGroup.ToArray())
                                                                         .ToArray();
            byte team = player.Require<TeamProperty>();
            KeyValuePair<Position3Int, Container>[] teamSpawns = spawns[team];
            int spawnIndex = Random.Range(0, teamSpawns.Length);
            return teamSpawns[spawnIndex].Key;
        }

        private void NextRound(SessionBase session, Container sessionContainer, SecureAreaComponent secureArea)
        {
            var scores = sessionContainer.Require<DualScoresComponent>();
            bool isLastRound = secureArea.roundTime.WithValue && scores.Sum(score => score.Value) == m_MaxRounds;
            if (isLastRound)
            {
                ForEachActivePlayer(session, sessionContainer, (playerId, player) => player.Require<FrozenProperty>().Value = true);
            }
            else
            {
                bool isFirstRound = secureArea.roundTime.WithoutValue;
                secureArea.roundTime.Value = m_RoundEndDurationUs + m_RoundDurationUs + m_BuyDurationUs;
                foreach (SiteComponent site in secureArea.sites)
                {
                    site.Zero();
                    site.timeUs.Value = m_SecureDurationUs;
                }
                ForEachActivePlayer(session, sessionContainer, (playerId, player) =>
                {
                    SpawnPlayer(session, sessionContainer, playerId, player);
                    if (isFirstRound)
                    {
                        var money = player.Require<MoneyComponent>();
                        money.count.Value = 800;
                        money.wantedBuyItemId.Clear();
                        InventoryComponent inventory = player.Require<InventoryComponent>().Zero();
                        PlayerItemManagerModiferBehavior.AddItems(inventory, ItemId.Pickaxe, ItemId.Pistol);
                    }
                    if (secureArea.lastWinningTeam.WithValue)
                    {
                        UShortProperty money = player.Require<MoneyComponent>().count;
                        money.Value += player.Require<TeamProperty>() == secureArea.lastWinningTeam ? m_RoundWinMoney : m_RoundLoseMoney;
                        if (money.Value > 7000) money.Value = 7000;
                    }
                });
            }
        }

        private static Queue<KeyValuePair<Position3Int, Container>>[] FindSpawns(ModelsProperty models)
            => models.Map.Where(modelPair => modelPair.Value.With<TeamProperty>())
                     .GroupBy(spawnTuple => spawnTuple.Value.Require<TeamProperty>().Value)
                     .OrderBy(group => group.Key)
                     .Select(teamGroup => new Queue<KeyValuePair<Position3Int, Container>>(teamGroup))
                     .ToArray();

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (session.IsLoading) return;

            base.Render(session, sessionContainer);

            SiteBehavior[] siteBehaviors = GetSiteBehaviors(sessionContainer.Require<VoxelMapNameProperty>());
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
                siteBehaviors[siteIndex].Render(secureArea.sites[siteIndex]);
        }

        public bool CanBuy(SessionBase session, Container sessionContainer)
        {
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            return secureArea.roundTime.WithValue && secureArea.roundTime > m_RoundEndDurationUs + m_RoundDurationUs;
        }

        public ushort GetCost(int itemId) => m_ItemPrices[itemId - 1];

        public override StringBuilder BuildUsername(StringBuilder builder, Container player)
        {
            string hex = GetHexColor(GetTeamColor(player.Require<TeamProperty>()));
            return builder.Append("<color=#").Append(hex).Append(">").AppendProperty(player.Require<UsernameProperty>()).Append("</color>");
        }

        public override Color GetTeamColor(int teamId) => teamId == BlueTeam ? m_BlueColor : m_RedColor;
    }
}