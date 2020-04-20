using System.Net;
using Swihoni.Components;

namespace Swihoni.Networking
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