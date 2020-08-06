using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
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
    [Serializable, Config(name: "sa")]
    public class SecureAreaConfig : ComponentBase
    {
        [Config(name: "end_duration")] public UIntProperty roundEndDurationUs;
        [Config(name: "duration")] public UIntProperty roundDurationUs;
        [Config(name: "buy_duration")] public UIntProperty buyDurationUs;
        [Config(name: "secure_duration")] public UIntProperty secureDurationUs;
        [Config] public ByteProperty playerCount;
        [Config(name: "win_bonus")] public UShortProperty roundWinMoney;
        [Config(name: "lose_bonus")] public UShortProperty roundLoseMoney;
        [Config(name: "kill_bonus")] public UShortProperty killMoney;
        [Config] public ByteProperty maxRounds;
    }

    [CreateAssetMenu(fileName = "Secure Area", menuName = "Session/Mode/Secure Area", order = 0)]
    public class SecureAreaMode : DeathmatchMode, IModeWithBuying
    {
        private const byte BlueTeam = 0, RedTeam = 1;
        private const int MaxMoney = 7000;

        private SiteBehavior[] m_SiteBehaviors;
        private VoxelMapNameProperty m_LastMapName;
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];

        [SerializeField] private Color m_BlueColor = new Color(0.1764705882f, 0.5098039216f, 0.8509803922f),
                                       m_RedColor = new Color(0.8196078431f, 0.2156862745f, 0.1960784314f);
        [SerializeField] private LayerMask m_PlayerTriggerMask = default;
        [SerializeField] private ushort[] m_ItemPrices = default;
        private SecureAreaConfig m_Config;

        public uint SecureDurationUs => m_Config.secureDurationUs;
        public uint RoundDurationUs => m_Config.roundDurationUs;
        public uint BuyDurationUs => m_Config.buyDurationUs;
        public uint RoundEndDurationUs => m_Config.roundEndDurationUs;

        public override void Initialize()
        {
            m_LastMapName = new VoxelMapNameProperty();
            m_Config = ConfigManager.Active.secureAreaConfig;
            SessionBase.RegisterSessionCommand("give_money");
        }

        private SiteBehavior[] GetSiteBehaviors(StringProperty mapName)
        {
            if (m_LastMapName == mapName) return m_SiteBehaviors;
            m_LastMapName.SetTo(mapName);
            return m_SiteBehaviors = MapManager.Singleton.Models.Values
                                               .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Site)
                                               .Cast<SiteBehavior>().ToArray();
        }

        public override uint ItemEntityLifespanUs => int.MaxValue;

        protected override void HandleAutoRespawn(in SessionContext context, HealthProperty health)
        {
            if (context.sessionContainer.Require<SecureAreaComponent>().roundTime.WithoutValue)
                base.HandleAutoRespawn(in context, health);
            else
            {
                // TODO:refactor this snippet is used multiple times
                if (health.IsAlive || context.player.Without(out RespawnTimerProperty respawn)) return;
                if (respawn.Value > context.durationUs) respawn.Value -= context.durationUs;
                else respawn.Value = 0u;
            }
        }

        protected override void KillPlayer(in DamageContext damageContext)
        {
            base.KillPlayer(damageContext);

            Container killer = damageContext.InflictingPlayer;
            if (damageContext.sessionContext.player.Require<TeamProperty>() != killer.Require<TeamProperty>())
            {
                UShortProperty money = killer.Require<MoneyComponent>().count;
                money.Value += m_Config.killMoney;
                if (money.Value > MaxMoney) money.Value = MaxMoney;
            }
        }

        public override void Modify(in SessionContext context)
        {
            base.Modify(context);

            Container sessionContainer = context.sessionContainer;
            uint durationUs = context.durationUs;
            var secureArea = sessionContainer.Require<SecureAreaComponent>();

            var players = sessionContainer.Require<PlayerContainerArrayElement>();
            int activePlayerCount = 0, redAlive = 0, blueAlive = 0;
            foreach (Container player in players)
            {
                var health = player.Require<HealthProperty>();
                if (health.WithoutValue) continue;

                activePlayerCount++;
                if (health.IsDead) continue;

                if (player.Require<TeamProperty>().TryWithValue(out byte team))
                    if (team == RedTeam) redAlive++;
                    else if (team == BlueTeam) blueAlive++;
            }

            SiteBehavior[] siteBehaviors = GetSiteBehaviors(sessionContainer.Require<VoxelMapNameProperty>());
            if (secureArea.roundTime.WithValue)
            {
                bool runTimer = true, redJustSecured = false, endedWithKills = false;
                bool inFightTime = secureArea.roundTime >= m_Config.roundEndDurationUs && secureArea.roundTime < m_Config.roundEndDurationUs + m_Config.roundDurationUs;
                if (inFightTime)
                {
                    if (activePlayerCount > 1)
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
                            secureArea.roundTime.Value = m_Config.roundEndDurationUs;
                    }
                    if (redAlive == 0 || blueAlive == 0) endedWithKills = true;

                    if (!endedWithKills)
                    {
                        for (var siteIndex = 0; siteIndex < siteBehaviors.Length; siteIndex++)
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
                                    Container player = context.GetModifyingPlayer(playerTrigger.PlayerId);
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
                                else if (secureArea.roundTime >= m_Config.roundEndDurationUs)
                                {
                                    // Round ended, site was secured by red
                                    site.timeUs.Value = 0u;
                                    secureArea.roundTime.Value = m_Config.roundEndDurationUs;
                                    sessionContainer.Require<DualScoresComponent>()[RedTeam].Value++;
                                    secureArea.lastWinningTeam.Value = RedTeam;
                                }
                                runTimer = redJustSecured = site.timeUs == 0u;
                            }
                            if (isRedInside && isBlueInside) runTimer = false; // Both in site
                        }
                    }
                }

                if (runTimer)
                {
                    if (secureArea.roundTime > durationUs)
                    {
                        if (!redJustSecured && !endedWithKills && secureArea.roundTime >= m_Config.roundEndDurationUs &&
                            secureArea.roundTime - durationUs < m_Config.roundEndDurationUs)
                        {
                            // Round just ended without contesting
                            sessionContainer.Require<DualScoresComponent>()[BlueTeam].Value++;
                        }
                        secureArea.roundTime.Value -= durationUs;
                    }
                    else NextRound(context, secureArea);
                }
                else
                {
                    if (secureArea.roundTime > m_Config.roundEndDurationUs && secureArea.roundTime - m_Config.roundEndDurationUs > durationUs)
                        secureArea.roundTime.Value -= durationUs;
                    else secureArea.roundTime.Value = m_Config.roundEndDurationUs;
                }
            }
            else
            {
                bool start = activePlayerCount == m_Config.playerCount;
                if (start) FirstRound(context, secureArea);
            }
        }

        private void FirstRound(in SessionContext context, SecureAreaComponent secureArea)
        {
            NextRound(context, secureArea);
            context.sessionContainer.Require<DualScoresComponent>().Zero();
        }

        public override void ModifyPlayer(in SessionContext context)
        {
            TimeUsProperty roundTime = context.sessionContainer.Require<SecureAreaComponent>().roundTime;
            bool isBuyTime = roundTime.WithValue && roundTime > m_Config.roundEndDurationUs + m_Config.roundDurationUs;
            Container player = context.player;
            if (isBuyTime) BuyingMode.HandleBuying(this, player, context.commands);
            player.Require<FrozenProperty>().Value = isBuyTime;

            if (PlayerModifierBehaviorBase.WithServerStringCommands(context, out IEnumerable<string[]> stringCommands))
            {
                foreach (string[] arguments in stringCommands)
                    switch (arguments[0])
                    {
                        case "give_money" when arguments.Length > 1 && ConfigManagerBase.Active.allowCheats && ushort.TryParse(arguments[1].Expand(), out ushort bonus):
                        {
                            player.Require<MoneyComponent>().count.Value += bonus;
                            break;
                        }
                        case "force_start":
                        {
                            FirstRound(context, context.sessionContainer.Require<SecureAreaComponent>());
                            break;
                        }
                    }
            }

            base.ModifyPlayer(in context);
        }

        protected override void SpawnPlayer(in SessionContext context, bool begin = false)
        {
            var secureArea = context.sessionContainer.Require<SecureAreaComponent>();
            Container player = context.player;
            if (begin) player.Require<TeamProperty>().Value = (byte) ((context.playerId + 1) % 2);
            if (secureArea.roundTime.WithValue)
            {
                player.ZeroIfWith<StatsComponent>();
                var health = player.Require<HealthProperty>();
                var money = player.Require<MoneyComponent>();
                var inventory = player.Require<InventoryComponent>();
                if (health.IsInactiveOrDead || money.count.WithoutValue)
                {
                    PlayerItemManagerModiferBehavior.Clear(inventory);
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pickaxe, 0);
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pistol, 1);
                    if (money.count.WithoutValue) money.count.Value = 800;
                }
                if (health.IsActiveAndAlive) PlayerItemManagerModiferBehavior.RefillAllAmmo(inventory);

                var move = player.Require<MoveComponent>();
                move.Zero();
                move.position.Value = GetSpawnPosition(context);
                player.ZeroIfWith<CameraComponent>();
                health.Value = 100;
                player.ZeroIfWith<RespawnTimerProperty>();
                player.Require<ByteIdProperty>().Value = 1;
            }
            else base.SpawnPlayer(in context, begin);
        }

        private static readonly List<int> RandomIndices = new List<int>();

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

                if (RandomIndices.Count == 0)
                {
                    RandomIndices.Capacity = teamSpawns.Length;
                    for (var i = 0; i < teamSpawns.Length; i++)
                        RandomIndices.Add(i);
                }
                int index = Random.Range(0, RandomIndices.Count);
                int spawnIndex = RandomIndices[index];
                RandomIndices.RemoveAt(index);

                return teamSpawns[spawnIndex].Key;
            }
            catch (Exception)
            {
                return GetRandomSpawn();
            }
        }

        private void NextRound(in SessionContext context, SecureAreaComponent secureArea)
        {
            var scores = context.sessionContainer.Require<DualScoresComponent>();
            bool isLastRound = secureArea.roundTime.WithValue && scores.Sum(score => score.Value) == m_Config.maxRounds;
            if (isLastRound)
            {
                context.ForEachActivePlayer((in SessionContext playerModifyContext) => playerModifyContext.player.Require<FrozenProperty>().Value = true);
            }
            else
            {
                secureArea.roundTime.Value = m_Config.roundEndDurationUs + m_Config.roundDurationUs + m_Config.buyDurationUs;
                foreach (SiteComponent site in secureArea.sites)
                {
                    site.Zero();
                    site.timeUs.Value = m_Config.secureDurationUs;
                }
                context.ForEachActivePlayer((in SessionContext playerModifyContext) =>
                {
                    SpawnPlayer(playerModifyContext);
                    if (secureArea.lastWinningTeam.WithValue)
                    {
                        Container player = playerModifyContext.player;
                        UShortProperty money = player.Require<MoneyComponent>().count;
                        money.Value += player.Require<TeamProperty>() == secureArea.lastWinningTeam ? m_Config.roundWinMoney : m_Config.roundLoseMoney;
                        if (money.Value > MaxMoney) money.Value = MaxMoney;
                    }
                });
                context.sessionContainer.Require<EntityArrayElement>().Clear();
            }
        }

        public override void Render(in SessionContext context)
        {
            if (context.session.IsLoading) return;

            base.Render(context);

            SiteBehavior[] siteBehaviors = GetSiteBehaviors(context.sessionContainer.Require<VoxelMapNameProperty>());
            var secureArea = context.sessionContainer.Require<SecureAreaComponent>();
            for (var siteIndex = 0; siteIndex < siteBehaviors.Length; siteIndex++)
                siteBehaviors[siteIndex].Render(secureArea.sites[siteIndex]);
        }

        public override bool CanSpectate(Container session, Container player)
        {
            if (session.Require<SecureAreaComponent>().roundTime.WithoutValue) return base.CanSpectate(session, player);
            return player.Require<HealthProperty>().IsDead && player.Require<RespawnTimerProperty>().Value < ConfigManagerBase.Active.respawnDuration / 2;
        }

        protected override float CalculateWeaponDamage(in PlayerHitContext context)
            => context.sessionContext.sessionContainer.Require<SecureAreaComponent>().roundTime.WithValue
            && context.hitPlayer.Require<TeamProperty>() == context.sessionContext.player.Require<TeamProperty>()
                ? 0.0f
                : base.CalculateWeaponDamage(context);

        public bool CanBuy(in SessionContext context, Container sessionLocalPlayer)
        {
            if (sessionLocalPlayer.Require<HealthProperty>().IsDead) return false;
            var secureArea = context.sessionContainer.Require<SecureAreaComponent>();
            return secureArea.roundTime.WithValue && secureArea.roundTime > m_Config.roundEndDurationUs + m_Config.roundDurationUs;
        }

        public ushort GetCost(int itemId) => m_ItemPrices[itemId - 1];

        public override Color GetTeamColor(byte? teamId) => teamId is byte team
            ? team == BlueTeam ? m_BlueColor : m_RedColor
            : Color.white;
    }
}