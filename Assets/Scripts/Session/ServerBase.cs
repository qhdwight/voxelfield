using System;
using System.Collections.Generic;
using System.Net;
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
        public ServerSessionContainer()
        {
        }

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

    public abstract class ServerBase : NetworkedSessionBase
    {
        private ComponentServerSocket m_Socket;

        protected ServerBase(IGameObjectLinker linker,
                             IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Loopback, 7777));
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_ClientCommandsContainer);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_ServerSessionContainer);
        }

        protected virtual void PreTick(Container tickSessionContainer)
        {
        }

        protected virtual void PostTick(Container tickSessionContainer)
        {
        }

        protected sealed override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            Container lastServerSessionContainer = m_SessionComponentHistory.Peek(),
                      serverSessionContainer = m_SessionComponentHistory.ClaimNext();
            serverSessionContainer.Reset();
            serverSessionContainer.MergeSet(lastServerSessionContainer);
            if (serverSessionContainer.If(out ServerStampComponent serverStampComponent))
            {
                serverStampComponent.tick.Value = tick;
                serverStampComponent.time.Value = time;
                float duration = time - lastServerSessionContainer.Require<ServerStampComponent>().time.OrElse(time);
                serverStampComponent.duration.Value = duration;
                PreTick(serverSessionContainer);
                Tick(serverSessionContainer);
                PostTick(serverSessionContainer);
            }
        }

        private void Tick(Container serverSessionComponent)
        {
            var serverPlayerComponents = serverSessionComponent.Require<PlayerContainerArrayProperty>();
            var i = 0;
            foreach (Container playerContainer in serverPlayerComponents)
            {
                if (playerContainer.If(out ClientStampComponent clientStampComponent))
                    clientStampComponent.duration.Value = 0u;
                var serverStampComponent = serverSessionComponent.Require<ServerStampComponent>();
                if (playerContainer.If(out ServerStampComponent playerServerStampComponent))
                    playerServerStampComponent.MergeSet(serverStampComponent);
                if (serverStampComponent.tick == 0u)
                {
                    if (playerContainer.If(out HealthProperty healthProperty))
                        healthProperty.Value = 100;
                    if (playerContainer.If(out InventoryComponent inventoryComponent))
                    {
                        PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 1);
                        PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 2);
                    }
                }
            }
            m_Socket.PollReceived((clientId, message) =>
            {
                switch (message)
                {
                    case ClientCommandsContainer clientCommandsContainer:
                    {
                        Container trustedPlayerComponent = serverPlayerComponents[clientId];
                        var playerCommandsStampComponent = clientCommandsContainer.Require<StampComponent>();
                        trustedPlayerComponent.MergeSet(clientCommandsContainer);
                        m_Modifier[clientId].ModifyChecked(trustedPlayerComponent, clientCommandsContainer, playerCommandsStampComponent.duration);
                        var clientStampComponent = trustedPlayerComponent.Require<ClientStampComponent>();
                        clientStampComponent.MergeSet(playerCommandsStampComponent);
                        // trustedPlayerComponent.Require<ServerStampComponent>().duration.Value += playerCommandsDuration;
                        // AnalysisLogger.AddDataPoint("", "A", trustedPlayerComponent.position.Value.x);
                        break;
                    }
                }
            });
            // if (m_Tick % 30 == 0)
            // {
            //     for (var i = 0; i < serverPlayerComponents.Length; i++)
            //     {
            //         Debug.Log(i + "," + serverPlayerComponents[i].Require<HealthProperty>().Value);
            //     }
            // }
            m_Socket.SendToAll(serverSessionComponent);
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}