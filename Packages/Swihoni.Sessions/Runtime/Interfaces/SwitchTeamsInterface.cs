using Swihoni.Components;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class SwitchTeamsInterface : SessionInterfaceBehavior
    {
        private byte? m_WantedTeam;

        public override void Render(SessionBase session, Container sessionContainer) { }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (m_WantedTeam is byte wantedTeam)
            {
                commands.Require<WantedTeamProperty>().Value = wantedTeam;
                m_WantedTeam = null;
            }
        }
    }
}