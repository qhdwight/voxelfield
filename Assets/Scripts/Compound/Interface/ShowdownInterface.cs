using System.Text;
using Compound.Session;
using Compound.Session.Mode;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Compound.Interface
{
    public class ShowdownInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_UpperText = default;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isShowdown = session.GetMode(sessionContainer) is ShowdownMode;
            if (isShowdown)
            {
                var showdown = sessionContainer.Require<ShowdownSessionComponent>();
                if (showdown.number.WithValue)
                {
                    uint totalUs = showdown.remainingUs;
                    bool isBuyPhase = totalUs > ShowdownMode.FightTimeUs;
                    if (isBuyPhase) totalUs -= ShowdownMode.FightTimeUs;
                    uint minutes = totalUs / 60_000_000u, seconds = totalUs / 1_000_000u % 60u;
                    void AppendTime(string prefix, StringBuilder builder)
                    {
                        builder.Append(prefix).Append(minutes).Append(":");
                        if (seconds < 10) builder.Append("0");
                        builder.Append(seconds);
                    }
                    m_UpperText.BuildText(builder => AppendTime(isBuyPhase ? "Buy! Time remaining until first stage: " : "Time to secure the cure: ", builder));
                }
                else
                {
                    m_UpperText.SetText("Warmup. Waiting for more players...");
                }
            }
            SetInterfaceActive(isShowdown);
        }
    }
}