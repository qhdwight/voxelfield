using Swihoni.Sessions.Components;
using Swihoni.Util.Interface;
using UnityEngine;

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

        public override void Render(KillFeedComponent kill)
        {
            bool isVisible = kill.elapsedUs > 0u;
            if (isVisible)
                m_Text.BuildText(builder => builder
                                           .Append(kill.killingPlayerId.Value)
                                           .Append(" [").Append("TEST").Append("] ")
                                           .Append(kill.killedPlayerId.Value));
            SetInterfaceActive(isVisible);
        }
    }
}