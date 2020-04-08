using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        private readonly Queue<ComponentBase> m_ReceivedMessages = new Queue<ComponentBase>();
        private readonly Dictionary<Type, Pool<ComponentBase>> m_MessagePools;

        private readonly Mutex m_Mutex = new Mutex();
        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback m_ReceiveCallback;

        protected ComponentSocketBase(IPEndPoint ip, Dictionary<Type, byte> codes)
        {
            m_Ip = ip;
            m_Codes = new DualDictionary<Type, byte>(codes);
            m_RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_ReadStream.SetLength(m_ReadStream.Capacity);
            m_Writer = new BinaryWriter(m_SendStream);
            m_Reader = new BinaryReader(m_ReadStream);
            m_MessagePools = codes.ToDictionary(pair => pair.Key,
                                                pair => new Pool<ComponentBase>(0, () => (ComponentBase) Activator.CreateInstance(pair.Key)));
        }

        public void StartReceiving()
        {
            m_ReceiveCallback = result =>
            {
                m_Mutex.WaitOne();
                try
                {
                    int received = m_RawSocket.EndReceiveFrom(result, ref m_ReceiveEndPoint);
                    m_ReadStream.Position = 0;
                    byte code = m_Reader.ReadByte();
                    Type type = m_Codes.GetReverse(code);
                    ComponentBase instance = m_MessagePools[type].Obtain();
                    Serializer.DeserializeInto(instance, m_ReadStream);
                    m_ReceivedMessages.Enqueue(instance);
                    m_RawSocket.BeginReceiveFrom(m_ReadStream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, null);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                    throw;
                }
                finally
                {
                    m_Mutex.ReleaseMutex();
                }
            };
            m_RawSocket.BeginReceiveFrom(m_ReadStream.GetBuffer(), 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, null);
        }

        public void PollReceived(Action<int, ComponentBase> received)
        {
            m_Mutex.WaitOne();
            try
            {
                while (m_ReceivedMessages.Count > 0)
                {
                    // TODO: change
                    ComponentBase message = m_ReceivedMessages.Dequeue();
                    received(1, message);
                    m_MessagePools[message.GetType()].Return(message);
                }
            }
            finally
            {
                m_Mutex.ReleaseMutex();
            }
        }

        public bool Send(ComponentBase message, IPEndPoint endPoint)
        {
            m_Mutex.WaitOne();
            try
            {
                byte code = m_Codes.GetForward(message.GetType());
                m_SendStream.Position = 0;
                m_Writer.Write(code);
                Serializer.SerializeFrom(message, m_SendStream);
                int sent = m_RawSocket.SendTo(m_SendStream.GetBuffer(), 0, (int) m_SendStream.Position, SocketFlags.None, endPoint);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                return false;
            }
            finally
            {
                m_Mutex.ReleaseMutex();
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