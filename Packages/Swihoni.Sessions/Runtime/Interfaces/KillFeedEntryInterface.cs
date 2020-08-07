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

        public override void Render(in SessionContext context, KillFeedComponent feed)
        {
            bool isVisible = feed.elapsedUs.WithValue;
            if (isVisible)
            {
                m_Text.StartBuild()
                      .AppendUsername(context, feed.killingPlayerId)
                      .Append(" [")
                      .AppendPropertyValue(feed.weaponName)
                      .Append("] ")
                      .Append(feed.isHeadShot ? "<sprite=0> " : string.Empty)
                      .AppendUsername(context, feed.killedPlayerId).Commit(m_Text);
            }
            SetInterfaceActive(isVisible);
        }
    }

    internal static class KillFeedExtensions
    {
        internal static StringBuilder AppendUsername(this StringBuilder builder, in SessionContext context, int playerId)
        {
            bool isLocalPlayer = context.sessionContainer.WithPropertyWithValue(out LocalPlayerId localPlayerId) && playerId == localPlayerId;
            if (isLocalPlayer) builder.Append("<b><i>");
            context.Mode.AppendUsername(builder, context.GetPlayer(playerId));
            if (isLocalPlayer) builder.Append("</i></b>");
            return builder;
        }
    }
}