using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ServerSessionContainer : Container
    {
        public ServerSessionContainer(IEnumerable<Type> types) : base(types)
        {
        }
    }

    [Serializable]
    public class ServerStampComponent : StampComponent
    {
    }

    [Serializable]
    public class ClientStampComponent : StampComponent
    {
    }

    public abstract class ServerBase : SessionBase
    {
        private ComponentServerSocket m_Socket;

        protected readonly CyclicArray<Container> m_SessionComponentHistory;

        private readonly IEnumerable<Type> m_ClientElements;

        protected ServerBase(IGameObjectLinker linker,
                             IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_SessionComponentHistory = new CyclicArray<Container>(250, () =>
            {
                var sessionContainer = new Container(sessionElements.Append(typeof(ServerStampComponent)));
                if (sessionContainer.If(out PlayerContainerArrayProperty playersProperty))
                    playersProperty.SetAll(() => new ServerSessionContainer(playerElements.Append(typeof(ServerStampComponent)).Append(typeof(ClientStampComponent))));
                return sessionContainer;
            });
            m_ClientElements = playerElements.Concat(commandElements).Append(typeof(StampComponent));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
            m_Socket.ResgisterMessage(typeof(ClientCommandsContainer), new ClientCommandsContainer(m_ClientElements));
        }

        protected virtual void PreTick(Container tickSessionComponent)
        {
        }

        protected virtual void PostTick(Container tickSessionComponent)
        {
        }

        protected sealed override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            Container lastTrustedSessionComponent = m_SessionComponentHistory.Peek(),
                      trustedSessionComponent = m_SessionComponentHistory.ClaimNext();
            trustedSessionComponent.Reset();
            trustedSessionComponent.MergeSet(lastTrustedSessionComponent);
            if (trustedSessionComponent.If(out ServerStampComponent stampComponent))
            {
                stampComponent.tick.Value = tick;
                stampComponent.time.Value = time;
                float duration = time - lastTrustedSessionComponent.Require<ServerStampComponent>().time.OrElse(time);
                stampComponent.duration.Value = duration;
                PreTick(trustedSessionComponent);
                Tick(trustedSessionComponent);
                PostTick(trustedSessionComponent);
            }
        }

        private void Tick(Container serverSessionComponent)
        {
            var serverPlayerComponents = serverSessionComponent.Require<PlayerContainerArrayProperty>();
            foreach (Container playerContainer in serverPlayerComponents)
            {
                if (playerContainer.If(out HealthProperty healthProperty))
                    healthProperty.Value = 100;
                if (playerContainer.If(out ClientStampComponent clientStampComponent))
                    clientStampComponent.duration.Value = 0u;
                var serverStampComponent = serverSessionComponent.Require<ServerStampComponent>();
                if (playerContainer.If(out ServerStampComponent playerServerStampComponent))
                    playerServerStampComponent.MergeSet(serverStampComponent);
                if (serverStampComponent.tick > 0u || !playerContainer.If(out InventoryComponent inventoryComponent)) continue;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 2);
            }
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandsContainer clientCommandsContainer:
                        Container trustedPlayerComponent = serverPlayerComponents[clientId];
                        var playerCommandsStampComponent = clientCommandsContainer.Require<StampComponent>();
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent, clientCommandsContainer, playerCommandsStampComponent.duration);
                        trustedPlayerComponent.MergeSet(clientCommandsContainer);
                        var clientStampComponent = trustedPlayerComponent.Require<ClientStampComponent>();
                        clientStampComponent.MergeSet(playerCommandsStampComponent);
                        // trustedPlayerComponent.Require<ServerStampComponent>().duration.Value += playerCommandsDuration;
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