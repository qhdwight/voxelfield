using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Secure Area", menuName = "Session/Mode/Secure Area", order = 0)]
    public class SecureAreaMode : DeathmatchMode, IModeWithBuying
    {
        private SiteBehavior[] m_SiteBehaviors;
        private VoxelMapNameProperty m_LastMapName;
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];

        [SerializeField] private LayerMask m_PlayerTriggerMask = default;
        [SerializeField] private uint m_RoundEndDurationUs = default, m_RoundDurationUs = default, m_BuyDurationUs = default, m_SecureDurationUs = default;
        [SerializeField] private byte m_Players = default;

        public uint SecureDurationUs => m_SecureDurationUs;
        public uint RoundDurationUs => m_RoundDurationUs;
        public uint BuyDurationUs => m_BuyDurationUs;
        public uint RoundEndDurationUs => m_RoundEndDurationUs;

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
            secureArea.teamScores.Clear();
            secureArea.sites.Clear();
        }

        public override void Modify(SessionBase session, Container sessionContainer, uint durationUs)
        {
            base.Modify(session, sessionContainer, durationUs);

            if (session.IsLoading) return;

            var secureArea = sessionContainer.Require<SecureAreaComponent>();

            SiteBehavior[] siteBehaviors = GetSiteBehaviors(sessionContainer.Require<VoxelMapNameProperty>());
            if (secureArea.roundTime.WithValue)
            {
                var canAdvance = true;
                bool isFightTime = secureArea.roundTime < m_RoundEndDurationUs + m_RoundDurationUs;
                if (isFightTime && (secureArea.roundTime > m_RoundEndDurationUs || secureArea.RedInside(out SiteComponent _)))
                {
                    for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
                    {
                        SiteBehavior siteBehavior = siteBehaviors[siteIndex];
                        SiteComponent site = secureArea.sites[siteIndex];
                        Bounds bounds = siteBehavior.Trigger.bounds;
                        int playersInsideCount = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, m_CachedColliders, siteBehavior.transform.rotation, m_PlayerTriggerMask);
                        bool isRedInside = false, isBlueInside = false;
                        for (var i = 0; i < playersInsideCount; i++)
                        {
                            Collider collider = m_CachedColliders[i];
                            if (collider.TryGetComponent(out PlayerTrigger playerTrigger))
                            {
                                Container player = session.GetModifyingPayerFromId(playerTrigger.PlayerId);
                                byte team = player.Require<TeamProperty>();
                                if (team == CtfMode.RedTeam) isRedInside = true;
                                else if (team == CtfMode.BlueTeam) isBlueInside = true;
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
                            }
                            canAdvance = site.timeUs == 0u;
                        }
                        if (isRedInside && isBlueInside) canAdvance = false; // Both in site
                    }
                }

                if (canAdvance)
                {
                    if (secureArea.roundTime > durationUs) secureArea.roundTime.Value -= durationUs;
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
                int playerCount = GetPlayerCount(sessionContainer);
                if (playerCount == m_Players)
                {
                    NextRound(session, sessionContainer, secureArea);
                    secureArea.teamScores.Zero();
                }
            }
        }

        protected override void SpawnPlayer(SessionBase session, Container sessionContainer, int playerId, Container player)
        {
            player.Require<TeamProperty>().Value = (byte) (playerId % 2 + 1);
            base.SpawnPlayer(session, sessionContainer, playerId, player);
        }

        private void NextRound(SessionBase session, Container sessionContainer, SecureAreaComponent secureArea)
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
                }
            });
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (session.IsLoading) return;

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
    }
}