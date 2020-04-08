using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class ComponentServerSocket : ComponentSocketBase
    {
        public ComponentServerSocket(IPEndPoint ip, Dictionary<Type, byte> codes) : base(ip, codes)
        {
            m_RawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_RawSocket.Bind(ip);
        }
    }
}