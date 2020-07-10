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
            bool isVisible = feed.elapsedUs > 0u;
            if (isVisible)
            {
                m_Text.StartBuild()
                      .AppendUsername(feed.killingPlayerId, sessionContainer)
                      .Append(" [")
                      .Append(feed.weaponName.Builder)
                      .Append("] ")
                      .Append(feed.isHeadShot ? "<color=#F25C69>[HS]</color> " : string.Empty)
                      .AppendUsername(feed.killedPlayerId, sessionContainer).Commit(m_Text);
            }
            SetInterfaceActive(isVisible);
        }
    }

    internal static class KillFeedExtensions
    {
        internal static StringBuilder AppendUsername(this StringBuilder builder, int playerId, Container session)
        {
            var username = session.GetPlayer(playerId).Require<UsernameProperty>();
            bool isLocalPlayer = session.WithPropertyWithValue(out LocalPlayerId localPlayerId) && playerId == localPlayerId;
            if (isLocalPlayer) builder.Append("<b><i>");
            builder.Append(username.Builder);
            if (isLocalPlayer) builder.Append("</i></b>");
            return builder;
        }
    }
}