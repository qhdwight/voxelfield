using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Swihoni.Components;
using Swihoni.Collections;
using UnityEngine;

namespace Swihoni.Networking
{
    public abstract class ComponentSocketBase : IDisposable
    {
        private const int BufferSize = 1 << 16;

        protected readonly IPEndPoint m_Ip;
        protected readonly Socket m_RawSocket;
        protected readonly Dictionary<IPEndPoint, Container> m_Connections = new Dictionary<IPEndPoint, Container>();

        private readonly DualDictionary<Type, byte> m_Codes;
        private readonly MemoryStream m_SendStream = new MemoryStream(BufferSize), m_ReadStream = new MemoryStream(BufferSize);
        private readonly BinaryWriter m_Writer;
        private readonly BinaryReader m_Reader;
        private readonly Dictionary<Type, Pool<ElementBase>> m_MessagePools = new Dictionary<Type, Pool<ElementBase>>();
        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public Dictionary<IPEndPoint, Container> Connections => m_Connections;

        protected ComponentSocketBase(IPEndPoint ip)
        {
            m_Ip = ip;
            m_Codes = new DualDictionary<Type, byte>();
            m_RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_ReadStream.SetLength(m_ReadStream.Capacity);
            m_Writer = new BinaryWriter(m_SendStream);
            m_Reader = new BinaryReader(m_ReadStream);
            // m_MessagePools = m_Codes.Forwards.ToDictionary(pair => pair.Key,
            //                                     pair => new Pool<ComponentBase>(0, () => (ComponentBase) Activator.CreateInstance(pair.Key)));
        }

        /// <summary>
        /// Register element type for serialization over network.
        /// Assigns an ID to each type. Order of registration is important.
        /// Container types must have an instance passed to figure out its children elements.
        /// The order of those element types is also important.
        /// </summary>
        public void RegisterMessage(Type elementType, Container container = null)
        {
            m_Codes.Add(elementType, (byte) m_Codes.Length);
            m_MessagePools[elementType] = new Pool<ElementBase>(1, () => elementType.IsContainer()
                                                                    ? container.Clone()
                                                                    : (ElementBase) Activator.CreateInstance(elementType));
        }

        public void PollReceived(Action<int, ElementBase> received)
        {
            while (m_RawSocket.Available > 0)
            {
                // TODO: performance use bytes received
                int bytesReceived = m_RawSocket.ReceiveFrom(m_ReadStream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint);
                if (!(m_ReceiveEndPoint is IPEndPoint ipEndPoint)) continue;
                // TODO: performance object pool
                if (!m_Connections.ContainsKey(ipEndPoint))
                {
                    var container = new Container(typeof(ByteProperty));
                    checked
                    {
                        var newConnectionId = (byte) (m_Connections.Count + 1);
                        container.Require<ByteProperty>().Value = newConnectionId;
                        Debug.Log($"[{GetType().Name}] Added player with id {newConnectionId}");
                    }
                    m_Connections.Add(ipEndPoint, container);
                }
                m_ReadStream.Position = 0;
                byte code = m_Reader.ReadByte();
                Type type = m_Codes.GetReverse(code);
                ElementBase message = m_MessagePools[type].Obtain();
                message.Reset();
                message.Deserialize(m_ReadStream);
                byte connectionId = m_Connections[ipEndPoint].Require<ByteProperty>();
                received(connectionId, message);
                m_MessagePools[message.GetType()].Return(message);
            }
        }

        public bool Send(ComponentBase message, IPEndPoint endPoint)
        {
            try
            {
                byte code = m_Codes.GetForward(message.GetType());
                m_SendStream.Position = 0;
                m_Writer.Write(code);
                message.Serialize(m_SendStream);
                int sent = m_RawSocket.SendTo(m_SendStream.GetBuffer(), 0, (int) m_SendStream.Position + 1, SocketFlags.None, endPoint);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                return false;
            }
        }

        public void Dispose()
        {
            m_RawSocket.Dispose();
            m_SendStream.Dispose();
            m_Writer.Dispose();
        }
    }
}