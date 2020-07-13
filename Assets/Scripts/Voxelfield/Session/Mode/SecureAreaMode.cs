using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Secure Area", menuName = "Session/Mode/Secure Area", order = 0)]
    public class SecureAreaMode : ModeBase
    {
        private SiteBehavior[] m_SiteBehaviors;
        private readonly VoxelMapNameProperty m_LastMapName = new VoxelMapNameProperty();
        private readonly Collider[] m_CachedColliders = new Collider[SessionBase.MaxPlayers];

        [SerializeField] private LayerMask m_PlayerTriggerMask = default;
        [SerializeField] private uint m_RoundEndDurationUs = default, m_RoundDurationUs = default, m_BuyDurationUs = default, m_SecureDurationUs = default;

        public uint SecureDurationUs => m_SecureDurationUs;
        public uint RoundDurationUs => m_RoundDurationUs;
        public uint BuyDurationUs => m_BuyDurationUs;
        public uint RoundEndDurationUs => m_RoundEndDurationUs;

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
            secureArea.roundNumber.Clear();
            secureArea.teamScores.Zero();
            secureArea.sites.Clear();
        }

        public override void Modify(SessionBase session, Container sessionContainer, uint durationUs)
        {
            base.Modify(session, sessionContainer, durationUs);

            if (session.IsPaused) return;

            var secureArea = sessionContainer.Require<SecureAreaComponent>();

            if (secureArea.roundNumber.WithValue)
            {
                for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
                {
                    SiteBehavior siteBehavior = m_SiteBehaviors[siteIndex];
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
                        if (site.timeUs > durationUs) site.timeUs.Value -= durationUs;
                        else
                        {
                            // Round ended, site was secured by red
                            site.timeUs.Value = 0u;
                            secureArea.roundTime.Value = m_RoundEndDurationUs;
                        }
                    }
                }

                if (secureArea.roundTime > durationUs) secureArea.roundTime.Value -= durationUs;
                else
                {
                    NextRound(session, sessionContainer, secureArea);
                }
            }
            else
            {
                // Waiting for players
                int playerCount = GetPlayerCount(sessionContainer);
                if (playerCount == 1)
                {
                    NextRound(session, sessionContainer, secureArea);
                }
            }
        }

        private void NextRound(SessionBase session, Container sessionContainer, SecureAreaComponent secureArea)
        {
            secureArea.roundTime.Value = m_RoundDurationUs + m_RoundEndDurationUs + m_BuyDurationUs;
            if (secureArea.roundNumber.WithValue) secureArea.roundNumber.Value++;
            else secureArea.roundTime.Value = 0;
            foreach (SiteComponent site in secureArea.sites)
            {
                site.Zero();
                site.timeUs.Value = m_SecureDurationUs;
            }
            ForEachActivePlayer(session, sessionContainer, (playerId, player) => SpawnPlayer(session, sessionContainer, playerId, player));
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
                m_SiteBehaviors[siteIndex].Render(secureArea.sites[siteIndex]);
        }
    }
}