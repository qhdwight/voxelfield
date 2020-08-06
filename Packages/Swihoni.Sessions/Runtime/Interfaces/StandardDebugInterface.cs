using System;
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
                                                 m_PacketLossText = default,
                                                 m_AllocationRateText = default,
                                                 m_PacketSizeText = default;
        [SerializeField] private float m_UpdateRate = 1.0f;
        private float m_LastUpdateTime;
        private long m_LastMemory;

        public override void Render(in SessionContext context)
        {
            float time = Time.realtimeSinceStartup, delta = time - m_LastUpdateTime;
            if (delta < m_UpdateRate) return;

            long totalMemory = GC.GetTotalMemory(false);
            float memoryRate = (totalMemory - m_LastMemory) / delta;
            m_LastMemory = totalMemory;

            var isPingVisible = false;
            if (context.session is Client && context.sessionContainer.WithPropertyWithValue(out LocalPlayerId localPlayer)
                                          && context.sessionContainer.GetPlayer(localPlayer).With(out StatsComponent stats) && stats.ping.WithValue)
            {
                isPingVisible = true;
                m_PingText.StartBuild().Append("Ping: ").Append(stats.ping).Append(" ms").Commit(m_PingText);
            }
            m_PingText.gameObject.SetActive(isPingVisible);

            var isPredictionVisible = false;
            if (context.session is Client client)
            {
                isPredictionVisible = true;
                m_PredictionErrorText.StartBuild().Append("Pred Err: ").Append(client.PredictionErrors).Commit(m_PredictionErrorText);
            }
            m_PredictionErrorText.gameObject.SetActive(isPredictionVisible);

            var areNetworkStatsVisible = false;

            if (context.session is NetworkedSessionBase networkSession && networkSession.Socket.NetworkManager.ConnectedPeersCount > 0)
            {
                areNetworkStatsVisible = true;
                m_ResetErrorText.StartBuild().Append("Rst Err: ").Append(networkSession.ResetErrors).Commit(m_ResetErrorText);
                m_UploadText.StartBuild().AppendFormat("Up: {0:F1} kb/s", networkSession.Socket.SendRateKbs).Commit(m_UploadText);
                m_DownloadText.StartBuild().AppendFormat("Down: {0:F1} kb/s", networkSession.Socket.ReceiveRateKbs).Commit(m_DownloadText);
                m_PacketLossText.StartBuild().AppendFormat("Drop: {0:P1}", networkSession.Socket.PacketLoss).Commit(m_PacketLossText);
                m_PacketSizeText.StartBuild().AppendFormat("Pkt: {0} bytes", networkSession.Socket.AveragePacketReceiveSize).Commit(m_PacketSizeText);
            }
            m_ResetErrorText.gameObject.SetActive(areNetworkStatsVisible);
            m_UploadText.gameObject.SetActive(areNetworkStatsVisible);
            m_DownloadText.gameObject.SetActive(areNetworkStatsVisible);
            m_PacketLossText.gameObject.SetActive(areNetworkStatsVisible);
            m_PacketSizeText.gameObject.SetActive(areNetworkStatsVisible);

            m_AllocationRateText.StartBuild().AppendFormat("Alloc: {0:F1} kb/s", memoryRate / 1000.0f).Commit(m_AllocationRateText);
            m_LastUpdateTime = time;
            SetInterfaceActive(true);
        }
    }
}