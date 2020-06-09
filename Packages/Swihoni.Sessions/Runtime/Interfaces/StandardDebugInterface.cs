using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class StandardDebugInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_UploadText = default,
                                                 m_DownloadText = default,
                                                 m_ResetErrorText = default,
                                                 m_PredictionErrorText = default,
                                                 m_PingText = default;
        [SerializeField] private float m_UpdateRate = 1.0f;
        private float m_LastUpdateTime;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            float time = Time.realtimeSinceStartup;
            if (time - m_LastUpdateTime < m_UpdateRate) return;

            if (sessionContainer.WithPropertyWithValue(out LocalPlayerProperty localPlayer) && sessionContainer.GetPlayer(localPlayer).With(out StatsComponent stats))
                m_PingText.BuildText(builder => builder.Append("Ping: ").Append(stats.ping).Append(" ms"));
            if (session is ClientBase client)
                m_PredictionErrorText.BuildText(builder => builder.Append("Prediction Errors: ").Append(client.PredictionErrors));
            if (session is NetworkedSessionBase networkSession)
            {
                m_ResetErrorText.BuildText(builder => builder.Append("Reset Errors: ").Append(networkSession.ResetErrors));
                m_UploadText.BuildText(builder => builder.AppendFormat("Up: {0:F1} kb/s", networkSession.Socket.SendRateKbs));
                m_DownloadText.BuildText(builder => builder.AppendFormat("Down: {0:F1} kb/s", networkSession.Socket.ReceiveRateKbs));
            }
            m_LastUpdateTime = time;
        }
    }
}