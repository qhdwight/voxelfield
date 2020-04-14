using System.Net;
using System.Net.Sockets;
using Components;

namespace Networking
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
            foreach (EndPoint endPoint in m_Connections)
            {
                Send(message, endPoint as IPEndPoint);
            }
        }
    }
}