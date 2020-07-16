using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions.Interfaces
{
    public class SpectatingInterface : SessionInterfaceBehavior
    {
        private BufferedTextGui m_Text;

        protected override void Awake()
        {
            base.Awake();
            m_Text = GetComponentInChildren<BufferedTextGui>();
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.WithPropertyWithValue(out SpectatingPlayerId spectatingPlayerId);
            if (isVisible)
            {
                StringBuilder builder = m_Text.StartBuild();
                builder.Append("Spectating: ");
                ModeManager.GetMode(sessionContainer).BuildUsername(builder, sessionContainer.GetPlayer(spectatingPlayerId));
                builder.Commit(m_Text);
            }
            SetInterfaceActive(isVisible);
        }
    }
}