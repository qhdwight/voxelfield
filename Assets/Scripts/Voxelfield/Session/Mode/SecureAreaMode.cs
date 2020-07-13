using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Secure Area", menuName = "Session/Mode/Secure Area", order = 0)]
    public class SecureAreaMode : ModeBase
    {
        private SiteBehavior[] m_SiteBehaviors;

        public override void Begin(SessionBase session, Container sessionContainer)
        {
            base.Begin(session, sessionContainer);
            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            secureArea.teamScores.Zero();
            secureArea.sites.Clear();
            m_SiteBehaviors = null;
            QuerySites();
        }

        public override void Modify(SessionBase session, Container sessionContainer, uint durationUs)
        {
            base.Modify(session, sessionContainer, durationUs);
            
            if (session.IsPaused) return;

            var secureArea = sessionContainer.Require<SecureAreaComponent>();
            for (var siteIndex = 0; siteIndex < secureArea.sites.Length; siteIndex++)
            {
                m_SiteBehaviors[siteIndex].Render(secureArea.sites[siteIndex]);
            }
        }

        private void QuerySites() => m_SiteBehaviors = MapManager.Singleton.Models.Values
                                                                 .Where(model => model.Container.Require<ModelIdProperty>() == ModelsProperty.Site)
                                                                 .Cast<SiteBehavior>().ToArray();
    }
}