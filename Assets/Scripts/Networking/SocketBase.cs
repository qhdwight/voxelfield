using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Collections;
using Serialization;

namespace Networking
{
    public abstract class SocketBase
    {
        private const int BufferSize = 1 << 16;

        private readonly Dictionary<Type, byte> m_Codes;
        private readonly UdpClient m_NetworkClient;
        private readonly Pool<byte[]> m_Buffers = new Pool<byte[]>(10, () => new byte[BufferSize], null);

        protected SocketBase(IPEndPoint endpoint, Dictionary<Type, byte> codes)
        {
            m_Codes = codes;
            m_NetworkClient = new UdpClient(endpoint);
        }

        public bool Send(object message)
        {
            byte[] buffer = m_Buffers.Obtain();
            int length = Serializer.Serialize(message, buffer);
            try
            {
                m_NetworkClient.Send(buffer, length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}