using System.Net;
using Networking;

namespace Session
{
    public abstract class ServerBase<TSessionComponent> : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        public override void Start()
        {
            var server = new ServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777), null);
            server.Receive();
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
        }
    }
}