using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface
{
    public class ScoreboardEntryInterface : DefaultScoreboardEntryInterface
    {
        protected override void PostRender(SessionBase session, Container sessionContainer, Container player)
        {
            if (sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Ctf)
            {
                m_UsernameText.StartBuild().AppendFormat("<color={0}>{1}</color>",
                                                         player.Require<TeamProperty>() == CtfMode.BlueTeam ? "blue" : "red",
                                                         player.Require<UsernameProperty>().Builder).Commit(m_UsernameText);
            }
        }
    }
}