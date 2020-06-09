using System.Net;
using LiteNetLib;

namespace Swihoni.Components.Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint ip, string key = null)
        {
            m_NetworkManager.Start();
            m_NetworkManager.Connect(ip, key);
        }

        public bool SendToServer(ElementBase element, DeliveryMethod deliveryMethod)
        {
            return m_NetworkManager.FirstPeer != null && Send(element, m_NetworkManager.FirstPeer, deliveryMethod);
        }
    }
}