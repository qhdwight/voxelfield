using System.Net;
using Networking;
using UnityEngine;

namespace Session
{
    public abstract class ServerBase<TSessionComponent>
        : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private ComponentServerSocket m_Socket;

        public override void Start()
        {
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777), TypeToId);
            m_Socket.StartReceiving();
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            m_Socket.PollReceived(@object =>
            {
                if (@object is PingCheckComponent pingCheckComponent)
                {
                }
            });
        }
    }
}