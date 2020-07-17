using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Util.Interface;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface.Showdown
{
    public class ShowdownInterface : RoundInterfaceBase
    {
        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Showdown;
            if (isVisible)
            {
                var showdown = sessionContainer.Require<ShowdownSessionComponent>();
                BuildUpperText(showdown);
                BuildLocalPlayer(session, sessionContainer, showdown);
            }
            SetInterfaceActive(isVisible);
        }

        private void BuildLocalPlayer(SessionBase session, Container sessionContainer, ShowdownSessionComponent showdown)
        {
            var isProgressVisible = false;
            if (showdown.number.WithValue && session.IsValidLocalPlayer(sessionContainer, out Container localPlayer))
            {
                var showdownPlayer = localPlayer.Require<ShowdownPlayerComponent>();
                uint securingElapsedUs = showdownPlayer.elapsedSecuringUs;
                if (securingElapsedUs > 0u)
                {
                    isProgressVisible = true;
                    m_SecuringProgress.Set(securingElapsedUs, ShowdownMode.SecureTimeUs);
                }
            }
            m_SecuringProgress.SetInterfaceActive(isProgressVisible);
        }

        private void BuildUpperText(ShowdownSessionComponent showdown)
        {
            if (showdown.number.WithValue)
            {
                uint displayTimeUs = showdown.remainingUs;
                bool isBuyPhase = displayTimeUs > ShowdownMode.FightTimeUs;
                if (isBuyPhase) displayTimeUs -= ShowdownMode.FightTimeUs;

                m_UpperText.StartBuild().Append(isBuyPhase ? "Buy! Time remaining until first stage: " : "Time to secure the cure: ").AppendTime(displayTimeUs).Commit(m_UpperText);
            }
            else
            {
                m_UpperText.SetText("Warmup. Waiting for more players...");
            }
        }
    }
}