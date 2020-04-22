using System.Net;
using System.Net.Sockets;

namespace Swihoni.Networking
{
    public class ComponentServerSocket : ComponentSocketBase
    {
        public ComponentServerSocket(IPEndPoint ip) : base(ip)
        {
            m_RawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_RawSocket.Bind(ip);
        }
    }
}