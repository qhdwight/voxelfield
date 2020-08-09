using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Swihoni.Components.Networking
{
    public class ComponentClientSocket : ComponentSocketBase
    {
        public ComponentClientSocket(IPEndPoint ip, NetDataWriter keyWriter = null)
        {
            m_NetworkManager.Start();
            m_NetworkManager.Connect(ip, keyWriter ?? new NetDataWriter());
        }

        public void SendToServer(ElementBase element, DeliveryMethod deliveryMethod, Action<ElementBase, NetDataWriter> serializeAction = null)
        {
            if (m_NetworkManager.FirstPeer != null) Send(element, m_NetworkManager.FirstPeer, deliveryMethod, serializeAction);
        }
    }
}