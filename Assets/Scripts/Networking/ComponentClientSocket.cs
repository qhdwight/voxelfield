using System;
using System.Collections.Generic;
using System.Net;
using Components;

namespace Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint endpoint, Dictionary<Type, byte> codes) : base(endpoint, codes)
        {
            m_RawSocket.Connect(endpoint);
        }

        public bool SendToServer(ComponentBase message)
        {
            return m_RawSocket.Connected && Send(message, m_Endpoint);
        }
    }
}