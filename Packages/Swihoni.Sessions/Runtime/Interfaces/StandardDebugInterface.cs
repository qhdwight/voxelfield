using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardDebugInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_UploadText, m_DownloadText, m_ResetErrorText, m_PredictionErrorText, m_PingText;
        private float m_LastUpdateTime;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            float time = Time.realtimeSinceStartup;
            if (time - m_LastUpdateTime > 2.0f)
            {
                if (sessionContainer.Present(out LocalPlayerProperty localPlayer) && sessionContainer.GetPlayer(localPlayer).Has(out StatsComponent stats))
                    m_PingText.SetText(builder => builder.Append("Ping: ").Append(stats.ping).Append(" ms"));
                if (session is ClientBase client)
                    m_PredictionErrorText.SetText(builder => builder.Append("Prediction Errors: ").Append(client.PredictionErrors));
                if (session is NetworkedSessionBase networkSession)
                {
                    m_ResetErrorText.SetText(builder => builder.Append("Reset Errors: ").Append(networkSession.ResetErrors));
                    m_UploadText.SetText(builder => builder.AppendFormat("Up: {0:F1} kb/s", networkSession.Socket.SendRate));
                    m_DownloadText.SetText(builder => builder.AppendFormat("Down: {0:F1} kb/s", networkSession.Socket.SendRate));
                }
                m_LastUpdateTime = time;
            }
        }
    }
}