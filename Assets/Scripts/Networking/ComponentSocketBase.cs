using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Collections;
using Components;
using UnityEngine;

namespace Networking
{
    public abstract class ComponentSocketBase : IDisposable
    {
        private const int BufferSize = 1 << 16;

        protected readonly IPEndPoint m_Ip;
        protected readonly Socket m_RawSocket;

        private readonly DualDictionary<Type, byte> m_Codes;
        private readonly MemoryStream m_SendStream = new MemoryStream(BufferSize), m_ReadStream = new MemoryStream(BufferSize);
        private readonly BinaryWriter m_Writer;
        private readonly BinaryReader m_Reader;
        private readonly Dictionary<Type, Pool<ComponentBase>> m_MessagePools = new Dictionary<Type, Pool<ComponentBase>>();

        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback m_ReceiveCallback;

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

        public void RegisterComponent(Type componentType)
        {
            m_MessagePools[componentType] = new Pool<ComponentBase>(1, () => (ComponentBase) Activator.CreateInstance(componentType));
        }

        public void PollReceived(Action<int, ComponentBase> received)
        {
            while (m_RawSocket.Available > 0)
            {
                // TODO: use bytes received for performance
                int bytesReceived = m_RawSocket.ReceiveFrom(m_ReadStream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint);
                m_ReadStream.Position = 0;
                byte code = m_Reader.ReadByte();
                Type type = m_Codes.GetReverse(code);
                ComponentBase message = m_MessagePools[type].Obtain();
                message.Reset();
                message.Deserialize(m_ReadStream);
                received(1, message);
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
                int sent = m_RawSocket.SendTo(m_SendStream.GetBuffer(), 0, (int) m_SendStream.Position, SocketFlags.None, endPoint);
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