using System.Net;
using Swihoni.Components;

namespace Swihoni.Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint ip) : base(ip) { }

        public bool SendToServer(ComponentBase message) { return Send(message, m_Ip); }
    }
}