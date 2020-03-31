using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Collections;
using Components;

namespace Networking
{
    public abstract class SocketBase
    {
        private const int BufferSize = 1 << 16;

        private readonly Dictionary<Type, byte> m_Codes;
        private readonly UdpClient m_NetworkClient;
        private readonly Pool<MemoryStream> m_Streams = new Pool<MemoryStream>(10, () => new MemoryStream(BufferSize), null);

        protected SocketBase(IPEndPoint endpoint, Dictionary<Type, byte> codes)
        {
            m_Codes = codes;
            m_NetworkClient = new UdpClient(endpoint);
        }

        public bool Send(object message)
        {
            MemoryStream stream = m_Streams.Obtain();
            Serializer.SerializeFrom(message, stream);
            try
            {
                m_NetworkClient.Send(stream.GetBuffer(), (int) stream.Position);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}