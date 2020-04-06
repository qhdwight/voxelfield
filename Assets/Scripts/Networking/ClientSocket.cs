using System;
using System.Collections.Generic;
using System.Net;

namespace Networking
{
    public class ClientSocket : SocketBase
    {
        public ClientSocket(IPEndPoint endpoint, Dictionary<Type, byte> codes) : base(endpoint, codes)
        {
            m_RawSocket.SendAsync(endpoint);
        }
    }
}