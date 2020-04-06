using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Collections;
using Components;

namespace Networking
{
    public class NetworkEvent
    {
        
    }
    
    public abstract class SocketBase
    {
        private const int BufferSize = 1 << 16;

        private readonly Dictionary<Type, byte> m_Codes;
        protected readonly Socket m_RawSocket;
        private readonly Pool<MemoryStream> m_Streams = new Pool<MemoryStream>(10, () => new MemoryStream(BufferSize));
        
        private readonly SocketState m_ReceiveState = new SocketState(), m_SendState = new SocketState();
        private EndPoint m_ReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback m_ReceiveCallback;

        protected SocketBase(IPEndPoint endpoint, Dictionary<Type, byte> codes)
        {
            m_Codes = codes;
            m_RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        private class SocketState
        {
            internal readonly byte[] buffer = new byte[BufferSize];
        }
        
        public virtual void Receive()
        {
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            
            m_RawSocket.ReceiveAsync()
            m_ReceiveCallback = result =>
            {
                var state = (SocketState) result.AsyncState;
                int received = m_RawSocket.EndReceiveFrom(result, ref m_ReceiveEndPoint);
                
                // Loop
                m_RawSocket.BeginReceiveFrom(state.buffer, 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, state);
            };
            m_RawSocket.BeginReceiveFrom(m_ReceiveState.buffer, 0, BufferSize, SocketFlags.None, ref m_ReceiveEndPoint, m_ReceiveCallback, m_ReceiveState);
        }

        public bool Send(object message)
        {
            MemoryStream stream = m_Streams.Obtain();
            Serializer.SerializeFrom(message, stream);
            try
            {
                m_RawSocket.BeginSend(stream.GetBuffer(), 0, (int) stream.Position, SocketFlags.None, result =>
                {
                    var s = (SocketState) result.AsyncState;
                    int sent = m_RawSocket.EndSend(result);
                }, m_SendState);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}