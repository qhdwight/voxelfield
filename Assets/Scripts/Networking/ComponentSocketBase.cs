using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        protected readonly IPEndPoint m_Endpoint;
        protected readonly Socket m_RawSocket;

        private readonly DualDictionary<Type, byte> m_Codes;
        private readonly MemoryStream m_SendStream = new MemoryStream(BufferSize);
        private readonly BinaryWriter m_SendWriter;
        private readonly ReadState m_ReadState = new ReadState();
        private readonly ConcurrentQueue<ComponentBase> m_ReceivedMessages = new ConcurrentQueue<ComponentBase>();
        private readonly Dictionary<Type, Pool<ComponentBase>> m_MessagePools;

        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback m_ReceiveCallback;

        protected ComponentSocketBase(IPEndPoint endpoint, Dictionary<Type, byte> codes)
        {
            m_Endpoint = endpoint;
            m_Codes = new DualDictionary<Type, byte>(codes);
            m_RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_SendWriter = new BinaryWriter(m_SendStream);
            m_MessagePools = codes.ToDictionary(pair => pair.Key,
                                                pair => new Pool<ComponentBase>(0, () => (ComponentBase) Activator.CreateInstance(pair.Key)));
        }

        protected class ReadState
        {
            internal readonly MemoryStream stream = new MemoryStream(BufferSize);

            internal readonly BinaryReader reader;

            internal ReadState()
            {
                reader = new BinaryReader(stream);
                stream.SetLength(BufferSize);
            }
        }

        public void StartReceiving()
        {
            m_ReceiveCallback = result =>
            {
                try
                {
                    var state = (ReadState) result.AsyncState;
                    int received = m_RawSocket.EndReceiveFrom(result, ref m_ReceiveEndPoint);
                    state.stream.Position = 0;
                    byte code = state.reader.ReadByte();
                    Type type = m_Codes.GetReverse(code);
                    ComponentBase instance = m_MessagePools[type].Obtain();
                    Serializer.DeserializeInto(instance, state.stream);
                    m_ReceivedMessages.Enqueue(instance);
                    m_RawSocket.BeginReceiveFrom(state.stream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, state);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                    throw;
                }
            };
            m_RawSocket.BeginReceiveFrom(m_ReadState.stream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, m_ReadState);
        }

        public void PollReceived(Action<int, ComponentBase> received)
        {
            while (m_ReceivedMessages.TryDequeue(out ComponentBase message))
            {
                received(0, message);
                m_MessagePools[message.GetType()].Return(message);
            }
        }

        public bool Send(ComponentBase message, IPEndPoint endPoint)
        {
            try
            {
                byte code = m_Codes.GetForward(message.GetType());
                m_SendStream.Position = 0;
                m_SendWriter.Write(code);
                Serializer.SerializeFrom(message, m_SendStream);

                m_RawSocket.BeginSendTo(m_SendStream.GetBuffer(), 0, (int) m_SendStream.Position, SocketFlags.None, endPoint, result =>
                {
                    int sent = m_RawSocket.EndSendTo(result);
                }, null);
                return true;
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
                return false;
            }
        }

        public void Dispose()
        {
            m_RawSocket.Dispose();
            m_SendStream.Dispose();
            m_SendWriter.Dispose();
        }
    }
}