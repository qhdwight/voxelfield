using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Util.Interface;
using Voxelfield.Interface.Showdown;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface
{
    public class SecureAreaInterface : RoundInterfaceBase
    {
        private SecureAreaMode m_SecureAreaMode;

        protected override void Awake()
        {
            base.Awake();
            m_SecureAreaMode = (SecureAreaMode) ModeManager.GetMode(ModeIdProperty.SecureArea);
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.SecureArea;
            if (isVisible)
            {
                var secureArea = sessionContainer.Require<SecureAreaComponent>();
                BuildUpperText(secureArea);
                BuildProgress(secureArea);
            }
            SetInterfaceActive(isVisible);
        }

        private void BuildProgress(SecureAreaComponent secureArea)
        {
            var isProgressVisible = false;
            if (secureArea.roundTime.WithValue && secureArea.RedInside(out SiteComponent site))
            {
                if (site.timeUs < m_SecureAreaMode.SecureDurationUs && site.timeUs > 0u)
                {
                    isProgressVisible = true;
                    m_SecuringProgress.Set(site.timeUs, m_SecureAreaMode.SecureDurationUs);
                }
            }
            m_SecuringProgress.SetInterfaceActive(isProgressVisible);
        }

        private void BuildUpperText(SecureAreaComponent secureArea)
        {
            if (secureArea.roundTime.WithValue)
            {
                uint buyTimeEndUs = m_SecureAreaMode.RoundEndDurationUs + m_SecureAreaMode.RoundDurationUs, displayTimeUs;
                StringBuilder builder = m_UpperText.StartBuild();
                if (secureArea.roundTime > buyTimeEndUs)
                {
                    displayTimeUs = secureArea.roundTime - buyTimeEndUs;
                    builder.Append("Buy! Time remaining until round start: ");
                }
                else if (secureArea.roundTime >= m_SecureAreaMode.RoundEndDurationUs)
                {
                    displayTimeUs = secureArea.roundTime - m_SecureAreaMode.RoundEndDurationUs;
                    const string text = "Round time left: ";
                    if (displayTimeUs > 10_000_000u) builder.Append(text);
                    else builder.Append("<color=red>").Append(text).Append("</color>");
                }
                else
                {
                    displayTimeUs = secureArea.roundTime;
                    builder.Append("Time until next round: ");
                }
                builder.AppendTime(displayTimeUs).Commit(m_UpperText);
            }
            else
            {
                m_UpperText.SetText("Warmup. Waiting for more players...");
            }
        }
    }
}