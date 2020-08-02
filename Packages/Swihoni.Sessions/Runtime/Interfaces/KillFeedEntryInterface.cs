using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions.Interfaces
{
    public class KillFeedEntryInterface : ElementInterfaceBase<KillFeedComponent>
    {
        private BufferedTextGui m_Text;

        protected override void Awake()
        {
            base.Awake();
            m_Text = GetComponentInChildren<BufferedTextGui>();
        }

        // private static StringBuilder GetName(int playerId, Container session) => new StringBuilder("ok");

        public override void Render(SessionBase session, Container sessionContainer, KillFeedComponent feed)
        {
            bool isVisible = feed.elapsedUs.WithValue;
            if (isVisible)
            {
                m_Text.StartBuild()
                      .AppendUsername(feed.killingPlayerId, sessionContainer)
                      .Append(" [")
                      .AppendPropertyValue(feed.weaponName)
                      .Append("] ")
                      .Append(feed.isHeadShot ? "<sprite=0> " : string.Empty)
                      .AppendUsername(feed.killedPlayerId, sessionContainer).Commit(m_Text);
            }
            SetInterfaceActive(isVisible);
        }
    }

    internal static class KillFeedExtensions
    {
        internal static StringBuilder AppendUsername(this StringBuilder builder, int playerId, Container sessionContainer)
        {
            Container player = sessionContainer.GetPlayer(playerId);
            bool isLocalPlayer = sessionContainer.WithPropertyWithValue(out LocalPlayerId localPlayerId) && playerId == localPlayerId;
            if (isLocalPlayer) builder.Append("<b><i>");
            SessionBase.BuildUsername(sessionContainer, builder, player);
            if (isLocalPlayer) builder.Append("</i></b>");
            return builder;
        }
    }
}