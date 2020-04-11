using System.Net;
using Components;

namespace Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint ip) : base(ip)
        {
            m_RawSocket.Connect(ip);
        }

        public bool SendToServer(ComponentBase message)
        {
            return m_RawSocket.Connected && Send(message, m_Ip);
        }
    }
}