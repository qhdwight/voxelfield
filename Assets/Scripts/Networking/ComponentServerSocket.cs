using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class ComponentServerSocket : ComponentSocketBase
    {
        public ComponentServerSocket(IPEndPoint endpoint, Dictionary<Type, byte> codes) : base(endpoint, codes)
        {
            m_RawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_RawSocket.Bind(endpoint);
        }
    }
}