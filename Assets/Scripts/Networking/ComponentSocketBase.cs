using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Collections;
using Components;
using UnityEngine;

namespace Networking
{
    public class NetworkEvent
    {
    }

    public abstract class ComponentSocketBase
    {
        private const int BufferSize = 1 << 16;

        protected readonly IPEndPoint m_Endpoint;
        protected readonly Socket m_RawSocket;

        private readonly DualDictionary<Type, byte> m_Codes;
        private readonly MemoryStream m_SendStream = new MemoryStream(BufferSize);
        private readonly BinaryWriter m_SendWriter;
        private readonly ReadState m_ReadState = new ReadState();
        private readonly ConcurrentQueue<object> m_ReceivedMessages = new ConcurrentQueue<object>();

        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback m_ReceiveCallback;

        protected ComponentSocketBase(IPEndPoint endpoint, Dictionary<Type, byte> codes)
        {
            m_Endpoint = endpoint;
            m_Codes = new DualDictionary<Type, byte>(codes);
            m_RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_SendWriter = new BinaryWriter(m_SendStream);
        }

        protected class ReadState
        {
            internal readonly MemoryStream stream = new MemoryStream(BufferSize);

            internal readonly BinaryReader reader;

            internal ReadState()
            {
                reader = new BinaryReader(stream);
            }
        }

        public void StartReceiving()
        {
            m_ReceiveCallback = result =>
            {
                var state = (ReadState) result.AsyncState;
                int received = m_RawSocket.EndReceiveFrom(result, ref m_ReceiveEndPoint);
                state.stream.Position = 0;
                state.stream.SetLength(received);
                byte code = state.reader.ReadByte();
                Type type = m_Codes.GetReverse(code);
                object instance = Activator.CreateInstance(type);
                Serializer.DeserializeInto(instance, state.stream);
                m_ReceivedMessages.Enqueue(instance);
                m_RawSocket.BeginReceiveFrom(state.stream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, state);
            };
            m_RawSocket.BeginReceiveFrom(m_ReadState.stream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, m_ReadState);
        }

        public void PollReceived(Action<object> received)
        {
            while (m_ReceivedMessages.TryDequeue(out object message))
                received(message);
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
    }
}