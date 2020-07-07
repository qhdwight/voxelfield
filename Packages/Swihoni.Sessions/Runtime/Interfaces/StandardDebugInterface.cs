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
                                                 m_PingText = default,
                                                 m_PacketLossText = default;
        [SerializeField] private float m_UpdateRate = 1.0f;
        private float m_LastUpdateTime;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            float time = Time.realtimeSinceStartup;
            if (time - m_LastUpdateTime < m_UpdateRate) return;

            if (sessionContainer.WithPropertyWithValue(out LocalPlayerId localPlayer)
             && sessionContainer.GetPlayer(localPlayer).With(out StatsComponent stats) && stats.ping.WithValue)
                m_PingText.StartBuild().Append("Ping: ").Append(stats.ping).Append(" ms").Commit(m_PingText);
            if (session is Client client)
                m_PredictionErrorText.StartBuild().Append("Pred Err: ").Append(client.PredictionErrors).Commit(m_PredictionErrorText);
            if (session is NetworkedSessionBase networkSession)
            {
                m_ResetErrorText.StartBuild().Append("Rst Err: ").Append(networkSession.ResetErrors).Commit(m_ResetErrorText);
                m_UploadText.StartBuild().AppendFormat("Up: {0:F1} kb/s", networkSession.Socket.SendRateKbs).Commit(m_UploadText);
                m_DownloadText.StartBuild().AppendFormat("Down: {0:F1} kb/s", networkSession.Socket.ReceiveRateKbs).Commit(m_DownloadText);
                m_PacketLossText.StartBuild().AppendFormat("Drop: {0:P1}", networkSession.Socket.PacketLoss).Commit(m_PacketLossText);
            }
            m_LastUpdateTime = time;
        }
    }
}