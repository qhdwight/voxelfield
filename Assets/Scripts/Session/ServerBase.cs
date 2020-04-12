using System;
using System.Collections.Generic;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;
using Session.Items.Modifiers;
using Session.Player.Components;
using Session.Player.Modifiers;

namespace Session
{
    [Serializable]
    public class ServerPlayerContainer : Container
    {
        public ServerPlayerContainer(IEnumerable<Type> types) : base(types)
        {
        }

        public StampComponent serverStamp, clientStamp;
    }

    [Serializable]
    public class ServerSessionContainer : Container
    {
    }

    public abstract class ServerBase : SessionBase
    {
        private ComponentServerSocket m_Socket;

        protected readonly CyclicArray<Container> m_SessionComponentHistory;

        protected ServerBase(IGameObjectLinker linker, List<Type> sessionElements, List<Type> playerElements, List<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_SessionComponentHistory = new CyclicArray<Container>(250, () =>
            {
                var sessionContainer = new Container(sessionElements);
                if (sessionContainer.With(out PlayerContainerArrayProperty playersProperty))
                    playersProperty.SetAll(() => new ServerPlayerContainer(playerElements));
                return sessionContainer;
            });
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
            m_Socket.RegisterComponent(typeof(ClientCommandsContainer));
        }

        protected virtual void PreTick(ServerSessionContainer tickSessionComponent)
        {
        }

        protected virtual void PostTick(ServerSessionContainer tickSessionComponent)
        {
        }

        protected sealed override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            ServerSessionContainer lastTrustedSessionComponent = m_SessionComponentHistory.Peek(),
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

        private void Tick(ServerSessionContainer trustedSessionComponent)
        {
            foreach (ServerPlayerContainer playerContainer in trustedSessionComponent.playerComponents)
            {
                if (playerContainer.WithProperty(out HealthProperty healthProperty))
                    healthProperty.Value = 100;
                playerContainer.clientStamp.duration.Value = 0u;
                playerContainer.serverStamp.MergeSet(trustedSessionComponent.stamp);
                if (trustedSessionComponent.stamp.tick > 0u || !playerContainer.WithComponent(out InventoryComponent inventoryComponent)) continue;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 2);
            }
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandsContainer clientCommands:
                        ServerPlayerContainer trustedPlayerComponent = trustedSessionComponent.playerComponents[clientId];
                        float playerCommandsDuration = clientCommands.stamp.duration;
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent.player, clientCommands.playerCommandsContainer, playerCommandsDuration);
                        trustedPlayerComponent.MergeSet(clientCommands.trustedPlayerContainer);
                        trustedPlayerComponent.clientStamp.duration.Value += playerCommandsDuration;
                        // AnalysisLogger.AddDataPoint("", "A", trustedPlayerComponent.position.Value.x);
                        break;
                }
            });
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}