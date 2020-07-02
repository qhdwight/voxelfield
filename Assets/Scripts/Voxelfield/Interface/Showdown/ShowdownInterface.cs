using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxel.Map;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface.Showdown
{
    public class ShowdownInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_UpperText = default;
        [SerializeField] private ProgressInterface m_SecuringProgress = default;
        
        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Showdown;
            if (isVisible)
            {
                var showdown = sessionContainer.Require<ShowdownSessionComponent>();
                BuildUpperText(showdown);
                BuildLocalPlayer(session, sessionContainer, showdown);
            }
            SetInterfaceActive(isVisible);
        }

        public static bool IsValidLocalPlayer(SessionBase session, Container sessionContainer, out Container localPlayer)
        {
            var localPlayerId = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerId.WithoutValue)
            {
                localPlayer = default;
                return false;
            }
            localPlayer = session.GetPlayerFromId(localPlayerId);
            return localPlayer.Require<HealthProperty>().IsAlive;
        }

        private void BuildLocalPlayer(SessionBase session, Container sessionContainer, ShowdownSessionComponent showdown)
        {
            var isProgressVisible = false;
            if (showdown.number.WithValue && IsValidLocalPlayer(session, sessionContainer, out Container localPlayer))
            {
                var showdownPlayer = localPlayer.Require<ShowdownPlayerComponent>();
                uint securingElapsedUs = showdownPlayer.elapsedSecuringUs;
                if (securingElapsedUs > 0u)
                {
                    isProgressVisible = true;
                    m_SecuringProgress.Set(securingElapsedUs, ShowdownMode.SecureTimeUs);
                }
            }
            m_SecuringProgress.SetInterfaceActive(isProgressVisible);
        }

        private void BuildUpperText(ShowdownSessionComponent showdown)
        {
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
    }
}