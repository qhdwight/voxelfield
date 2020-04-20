using System.Net;
using System.Net.Sockets;
using Swihoni.Components;

namespace Swihoni.Networking
{
    public class ComponentServerSocket : ComponentSocketBase
    {
        public ComponentServerSocket(IPEndPoint ip) : base(ip)
        {
            m_RawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_RawSocket.Bind(ip);
        }

        public void SendToAll(ComponentBase message)
        {
            foreach (IPEndPoint endPoint in m_Connections.Keys)
                Send(message, endPoint);
        }
    }
}