using System;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;
using Session.Items.Modifiers;
using Session.Player.Modifiers;
using Util;

namespace Session
{
    [Serializable]
    public class ServerPlayerContainer : ContainerBase
    {
        public ContainerBase playerComponent;
        public StampComponent serverStamp, clientStamp;
    }
    
    public abstract class ServerBase<TSessionContainer> : SessionBase<TSessionContainer>
        where TSessionContainer : SessionContainerBase, new()
    {
        private ComponentServerSocket m_Socket;
        
        protected readonly CyclicArray<TSessionContainer> m_SessionComponentHistory = new CyclicArray<TSessionContainer>(250, () =>
        {
            return new TSessionContainer {PlayerComponents = new ArrayProperty<ServerPlayerContainer>(MaxPlayers)};
        });

        protected ServerBase(IGameObjectLinker linker) : base(linker)
        {
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
        }

        protected virtual void PreTick(TSessionContainer tickSessionComponent)
        {
        }

        protected virtual void PostTick(TSessionContainer tickSessionComponent)
        {
        }

        protected sealed override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            TSessionContainer lastTrustedSessionComponent = m_SessionComponentHistory.Peek(),
                              trustedSessionComponent = m_SessionComponentHistory.ClaimNext();
            trustedSessionComponent.Reset();
            trustedSessionComponent.MergeSet(lastTrustedSessionComponent);
            trustedSessionComponent.stamp.tick.Value = tick;
            trustedSessionComponent.stamp.time.Value = time;
            float duration = time - lastTrustedSessionComponent.stamp.time.OrElse(time);
            trustedSessionComponent.stamp.duration.Value = duration;
            PreTick(trustedSessionComponent);
            Tick(trustedSessionComponent);
            PostTick(trustedSessionComponent);
        }

        private void Tick(TSessionContainer trustedSessionComponent)
        {
            foreach (ContainerBase playerContainer in trustedSessionComponent.PlayerComponents)
            {
                playerContainer.health.Value = 100;
                playerContainer.stamp.duration.Value = 0u;
                playerContainer.stamp.time = trustedSessionComponent.stamp.time;
                if (trustedSessionComponent.stamp.tick > 0u) continue;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(playerContainer.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(playerContainer.inventory, ItemId.TestingRifle, 2);
            }
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandComponent commands:
                        StampedPlayerComponent trustedPlayerComponent = trustedSessionComponent.playerComponents[clientId];
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent, commands);
                        trustedPlayerComponent.MergeSet(commands.trustedComponent);
                        trustedPlayerComponent.stamp.duration.Value += commands.duration;
                        AnalysisLogger.AddDataPoint("", "A", trustedPlayerComponent.position.Value.x);
                        break;
                }
            });
            DebugBehavior.Singleton.Server = trustedSessionComponent;
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}