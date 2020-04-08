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
        }

        protected virtual void PreTick(TSessionComponent tickSessionComponent)
        {
        }

        protected virtual void PostTick(TSessionComponent tickSessionComponent)
        {
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            TSessionComponent lastTrustedSessionComponent = m_SessionComponentHistory.Peek(),
                              trustedSessionComponent = m_SessionComponentHistory.ClaimNext();
            trustedSessionComponent.Zero();
            trustedSessionComponent.MergeSet(lastTrustedSessionComponent);
            trustedSessionComponent.stamp.tick.Value = tick;
            trustedSessionComponent.stamp.time.Value = time;
            float duration = time - lastTrustedSessionComponent.stamp.time.OrElse(time);
            trustedSessionComponent.stamp.duration.Value = duration;
            PreTick(trustedSessionComponent);

            foreach (PlayerComponent playerComponent in trustedSessionComponent.playerComponents)
            {
                playerComponent.health.Value = 100;
                if (tick > 0) continue;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(playerComponent.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(playerComponent.inventory, ItemId.TestingRifle, 2);
            }
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandComponent commands:
                        PlayerComponent trustedPlayerComponent = trustedSessionComponent.playerComponents[clientId];
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent, commands);
                        trustedPlayerComponent.MergeSet(commands.trustedComponent);
                        break;
                }
            });
            DebugBehavior.Singleton.Server = trustedSessionComponent;

            PostTick(trustedSessionComponent);
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}