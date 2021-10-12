using System.Net;
using LiteNetLib;

namespace Swihoni.Components.Networking
{
    public class ComponentServerSocket : ComponentSocketBase
    {
        public ComponentServerSocket(IPEndPoint ip, EventBasedNetListener.OnConnectionRequest acceptConnection = null)
        {
            acceptConnection ??= DefaultAcceptConnection;
            m_NetworkManager.Start(ip.Address, null, ip.Port);
            m_Listener.ConnectionRequestEvent += acceptConnection;
        }

        private static void DefaultAcceptConnection(ConnectionRequest request) => request.Accept();
    }
}