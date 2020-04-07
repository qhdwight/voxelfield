using System.Net;
using Networking;
using Session.Player;
using UnityEngine;

namespace Session
{
    public abstract class ServerBase<TSessionComponent>
        : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private ComponentServerSocket m_Socket;

        protected ServerBase(IGameObjectLinker linker) : base(linker)
        {
        }
        
        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777), TypeToId);
            m_Socket.StartReceiving();
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            m_Socket.PollReceived(message =>
            {
                switch (message)
                {
                    case PlayerCommandsComponent commands:
                        
                        break;
                }
            });
        }
    }
}