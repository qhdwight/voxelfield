using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Layers;
using LiteNetLib.Utils;
using Swihoni.Collections;
using UnityEngine;

namespace Swihoni.Components.Networking
{
    public abstract class ComponentSocketBase : IDisposable
    {
        private const int InitialBufferSize = 1 << 16;

        protected readonly NetManager m_NetworkManager;

        private readonly DualDictionary<Type, byte> m_Codes = new DualDictionary<Type, byte>();
        private readonly Dictionary<byte, byte> m_CodeToChannel = new Dictionary<byte, byte>();
        private readonly Dictionary<Type, Pool<ElementBase>> m_MessagePools = new Dictionary<Type, Pool<ElementBase>>();
        private readonly NetDataWriter m_Writer = new NetDataWriter(true, InitialBufferSize);
        private readonly float m_StartTime;
        protected readonly EventBasedNetListener m_Listener = new EventBasedNetListener();
        private Action<NetPeer, ElementBase> m_OnReceive;
        public NetManager NetworkManager => m_NetworkManager;
        public EventBasedNetListener Listener => m_Listener;

        public float SendRateKbs => m_NetworkManager.Statistics.BytesSent / (Time.realtimeSinceStartup - m_StartTime) * 0.001f;
        public float ReceiveRateKbs => m_NetworkManager.Statistics.BytesReceived / (Time.realtimeSinceStartup - m_StartTime) * 0.001f;

        protected ComponentSocketBase()
        {
            m_NetworkManager = new NetManager(m_Listener, new Crc32cLayer())
            {
                EnableStatistics = true,
                UpdateTime = 1,
                ChannelsCount = 4,
                IPv6Enabled = false,
                ReuseAddress = true,
            };
            m_Listener.NetworkReceiveEvent += OnReceive;
            m_Listener.PeerConnectedEvent += peer => Debug.Log($"[{GetType().Name}] Connected: {peer.EndPoint}");
            m_Listener.PeerDisconnectedEvent += (peer, info) => Debug.Log($"[{GetType().Name}] Disconnected: {peer.EndPoint}");
            m_StartTime = Time.realtimeSinceStartup;
        }

        private void OnReceive(NetPeer fromPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte code = reader.GetByte();
            Type type = m_Codes.GetReverse(code);
            ElementBase message = m_MessagePools[type].Obtain();
            message.Deserialize(reader);
            m_OnReceive?.Invoke(fromPeer, message);
            m_MessagePools[message.GetType()].Return(message);
            reader.Recycle();
        }

        public void PollEvents() => m_NetworkManager.PollEvents();

        public void PollReceived(Action<NetPeer, ElementBase> onReceive)
        {
            m_OnReceive = onReceive;
            m_NetworkManager.PollEvents();
        }

        /// <summary>
        /// Register element type for serialization over network.
        /// Assigns an ID to each type. Order of registration is important.
        /// </summary>
        public void RegisterSimpleElement(Type registerType, byte channel = 0)
        {
            var code = (byte) m_Codes.Length;
            m_Codes.Add(registerType, code);
            m_CodeToChannel.Add(code, channel);
            m_MessagePools[registerType] = new Pool<ElementBase>(1, () => (ElementBase) Activator.CreateInstance(registerType));
        }

        /// <summary>
        /// <see cref="RegisterSimpleElement"/>
        /// </summary>
        public void RegisterContainer(Type registerType, Container container, byte channel = 0)
        {
            var code = (byte) m_Codes.Length;
            m_Codes.Add(registerType, code);
            m_CodeToChannel.Add(code, channel);
            m_MessagePools[registerType] = new Pool<ElementBase>(1, container.Clone);
        }

        public bool Send(ElementBase element, NetPeer peer, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte code = m_Codes.GetForward(element.GetType());
                m_Writer.Reset();
                m_Writer.Put(code);
                element.Serialize(m_Writer);
                peer.Send(m_Writer, m_CodeToChannel[code], deliveryMethod);
                return true;
            }
            catch (KeyNotFoundException keyNotFoundException)
            {
                throw new Exception("Type has not been registered to send across socket!", keyNotFoundException);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                return false;
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
        }
    }
}