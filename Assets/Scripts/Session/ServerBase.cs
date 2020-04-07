using System.Net;
using Components;
using Networking;
using Session.Items.Modifiers;
using Session.Player.Components;
using Session.Player.Modifiers;

namespace Session
{
    public abstract class ServerBase<TSessionComponent>
        : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private ComponentServerSocket m_Socket;

        protected readonly SessionComponentHistory<TSessionComponent> m_SessionComponentHistory = new SessionComponentHistory<TSessionComponent>();

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
            TSessionComponent trustedSessionComponent = m_SessionComponentHistory.ClaimNext();
            Extensions.Emptify(trustedSessionComponent);
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandComponent commands:
                        PlayerComponent trustedPlayerComponent = trustedSessionComponent.playerComponents[clientId];
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent, commands);
                        Copier.MergeSet(trustedPlayerComponent, commands.trustedComponent);
                        break;
                }
            });
            foreach (PlayerComponent playerComponent in trustedSessionComponent.playerComponents)
            {
                playerComponent.health.Value = 100;
                if (tick == 0)
                {
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(playerComponent.inventory, ItemId.TestingRifle, 1);
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(playerComponent.inventory, ItemId.TestingRifle, 2);
                }
            }
            DebugBehavior.Singleton.Server = trustedSessionComponent;
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}