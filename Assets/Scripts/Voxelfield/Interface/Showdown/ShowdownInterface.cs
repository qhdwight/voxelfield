using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface.Showdown
{
    public class ShowdownInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_UpperText = default;
        [SerializeField] private Slider m_SecuringProgress = default;
        private BufferedTextGui m_SecuringText;
        private CanvasGroup m_SecuringGroup;

        protected override void Awake()
        {
            base.Awake();
            m_SecuringText = m_SecuringProgress.GetComponentInChildren<BufferedTextGui>();
            m_SecuringGroup = m_SecuringProgress.GetComponent<CanvasGroup>();
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = session.GetMode(sessionContainer) is ShowdownMode;
            if (isVisible)
            {
                var showdown = sessionContainer.Require<ShowdownSessionComponent>();
                BuildUpperText(showdown);
                BuildLocalPlayer(session, sessionContainer, showdown);
            }
            SetInterfaceActive(isVisible);
        }

        private static bool IsValidLocalPlayer(SessionBase session, Container sessionContainer, out Container localPlayer)
        {
            localPlayer = default;
            var localPlayerId = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerId.WithoutValue) return false;
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
                isProgressVisible = securingElapsedUs > 0u;
                if (isProgressVisible)
                {
                    m_SecuringProgress.value = (float) (securingElapsedUs / (decimal) ShowdownMode.SecureTimeUs);
                    m_SecuringText.BuildText(builder => builder.AppendFormat("{0:F1} Seconds", securingElapsedUs * TimeConversions.MicrosecondToSecond));
                }
            }
            SetCanvasGroupActive(m_SecuringGroup, isProgressVisible);
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