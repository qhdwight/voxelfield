using Swihoni.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class SwitchTeamsInterface : SessionInterfaceBehavior
    {
        private byte? m_WantedTeam;

        public override void Render(in SessionContext context) { SetInterfaceActive(NoInterrupting && InputProvider.GetInput(InputType.SwitchTeams)); }

        public void WantedTeamButton(int wantedTeam) => m_WantedTeam = (byte) wantedTeam;

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (!(m_WantedTeam is { } wantedTeam)) return;

            commands.Require<WantedTeamProperty>().Value = wantedTeam;
            m_WantedTeam = null;
        }
    }
}