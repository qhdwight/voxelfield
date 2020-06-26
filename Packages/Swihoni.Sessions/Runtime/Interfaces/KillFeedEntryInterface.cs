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

        private static StringBuilder GetName(int playerId, Container session)
            => session.Require<PlayerContainerArrayElement>()[playerId].Require<UsernameElement>().GetString();

        // private static StringBuilder GetName(int playerId, Container session) => new StringBuilder("ok");

        public override void Render(Container session, KillFeedComponent feed)
        {
            bool isVisible = feed.elapsedUs > 0u;
            if (isVisible)
            {
                void Build(StringBuilder builder) => builder.Append(GetName(feed.killingPlayerId, session))
                                                            .Append(" [")
                                                            .Append(feed.weaponName.GetString())
                                                            .Append("] ")
                                                            .Append(GetName(feed.killedPlayerId, session));
                m_Text.BuildText(Build);
            }
            SetInterfaceActive(isVisible);
        }
    }
}