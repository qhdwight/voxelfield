using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Swihoni.Components.Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint ip, string key = null)
        {
            m_NetworkManager.Start();
            m_NetworkManager.Connect(ip, key);
        }

        public bool SendToServer(ElementBase element, DeliveryMethod deliveryMethod, Action<ElementBase, NetDataWriter> serializeAction = null)
            => m_NetworkManager.FirstPeer != null && Send(element, m_NetworkManager.FirstPeer, deliveryMethod, serializeAction);
    }
}