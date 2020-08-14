using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Layers;
using LiteNetLib.Utils;
using Swihoni.Collections;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Components.Networking
{
    public interface IReceiver
    {
        void OnReceive(NetPeer peer, NetDataReader reader, byte code);
    }

    public abstract class ComponentSocketBase : IDisposable
    {
        private const int InitialBufferSize = 1 << 16;

        protected readonly NetManager m_NetworkManager;

        private readonly DualDictionary<Type, byte> m_Codes = new DualDictionary<Type, byte>();
        private readonly Dictionary<byte, byte> m_CodeToChannel = new Dictionary<byte, byte>();
        private readonly NetDataWriter m_Writer = new NetDataWriter(true, InitialBufferSize);
        private readonly float m_StartTime;
        protected readonly EventBasedNetListener m_Listener = new EventBasedNetListener();
        public IReceiver Receiver { get; set; }
        public NetManager NetworkManager => m_NetworkManager;
        public EventBasedNetListener Listener => m_Listener;

        public float SendRateKbs => m_NetworkManager.Statistics.BytesSent / (Time.realtimeSinceStartup - m_StartTime) * 0.001f;
        public float ReceiveRateKbs => m_NetworkManager.Statistics.BytesReceived / (Time.realtimeSinceStartup - m_StartTime) * 0.001f;
        public float PacketLoss
            => (float) (m_NetworkManager.Statistics.PacketsSent == 0 ? 0 : m_NetworkManager.Statistics.PacketLoss / (double) m_NetworkManager.Statistics.PacketsSent);
        public int AveragePacketReceiveSize => (int) (m_NetworkManager.Statistics.BytesReceived / (double) m_NetworkManager.Statistics.PacketsReceived);

        protected ComponentSocketBase()
        {
            m_NetworkManager = new NetManager(m_Listener, new Crc32cLayer())
            {
                EnableStatistics = true,
                UpdateTime = 1,
                ChannelsCount = 4,
                IPv6Enabled = IPv6Mode.Disabled,
                ReuseAddress = true,
#if UNITY_EDITOR
                DisconnectTimeout = int.MaxValue,
#endif
            };
            m_Listener.NetworkReceiveEvent += Receive;
            m_Listener.PeerConnectedEvent += peer => Debug.Log($"[{GetType().Name}] Connected: {peer.EndPoint}");
            m_Listener.PeerDisconnectedEvent += (peer, info) => Debug.Log($"[{GetType().Name}] Disconnected: {peer.EndPoint}");
            m_StartTime = Time.realtimeSinceStartup;
        }

        private void Receive(NetPeer fromPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte code = reader.GetByte();
                Receiver?.OnReceive(fromPeer, reader, code);
            }
            finally
            {
                reader.Recycle();
            }
        }

        public void PollEvents() => m_NetworkManager.PollEvents();

        /// <summary>
        /// Register element type for serialization over network.
        /// Assigns an ID to each type. Order of registration is important.
        /// </summary>
        public byte Register(Type registerType, byte channel, byte code)
        {
            m_Codes.Add(registerType, code);
            m_CodeToChannel.Add(code, channel);
            return code;
        }

        public void Send(ElementBase element, NetPeer peer, DeliveryMethod deliveryMethod, Action<ElementBase, NetDataWriter> serializeAction = null)
        {
            try
            {
                byte code = m_Codes.GetForward(element.GetType());
                m_Writer.Reset();
                m_Writer.Put(code);
                if (serializeAction == null) element.Serialize(m_Writer);
                else serializeAction(element, m_Writer);
                peer.Send(m_Writer, m_CodeToChannel[code], deliveryMethod);
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Type has not been registered to send across socket!");
            }
            catch (Exception exception)
            {
                L.Exception(exception, "Failed to send");
                throw;
            }
        }

        public void Dispose()
        {
            m_Listener.ClearNetworkReceiveEvent();
            m_Listener.ClearConnectionRequestEvent();
            m_Listener.ClearPeerConnectedEvent();
            m_Listener.ClearPeerDisconnectedEvent();
            m_Listener.ClearNetworkLatencyUpdateEvent();
            m_NetworkManager.Stop();
            Receiver = null;
        }
    }
}