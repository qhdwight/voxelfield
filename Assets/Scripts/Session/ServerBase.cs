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

        protected virtual void PreTick(Container tickSession)
        {
        }

        protected virtual void PostTick(Container tickSession)
        {
        }

        protected sealed override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            Container previousServerSession = m_SessionComponentHistory.Peek(),
                      serverSession = m_SessionComponentHistory.ClaimNext();
            serverSession.CopyFrom(previousServerSession);
            if (serverSession.If(out ServerStampComponent serverStamp))
            {
                serverStamp.tick.Value = tick;
                serverStamp.time.Value = time;
                float duration = time - previousServerSession.Require<ServerStampComponent>().time.OrElse(time);
                serverStamp.duration.Value = duration;
                PreTick(serverSession);
                Tick(serverSession);
                PostTick(serverSession);
            }
        }

        private void Tick(Container serverSession)
        {
            var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
            foreach (Container serverPlayer in serverPlayers)
            {
                if (serverPlayer.If(out ClientStampComponent clientStamp))
                    clientStamp.duration.Value = 0u;
                var serverStampComponent = serverSession.Require<ServerStampComponent>();
                if (serverPlayer.If(out ServerStampComponent serverStamp))
                    serverStamp.CopyFrom(serverStampComponent);
                if (serverStampComponent.tick == 0u)
                {
                    if (serverPlayer.If(out HealthProperty healthProperty))
                        healthProperty.Value = 100;
                    if (serverPlayer.If(out InventoryComponent inventoryComponent))
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
                    case ClientCommandsContainer clientCommands:
                    {
                        Container serverPlayer = serverPlayers[clientId];
                        var clientStamp = clientCommands.Require<StampComponent>();
                        serverPlayer.MergeSet(clientCommands);
                        m_Modifier[clientId].ModifyChecked(serverPlayer, clientCommands, clientStamp.duration);
                        var serverClientStamp = serverPlayer.Require<ClientStampComponent>();
                        serverClientStamp.CopyFrom(clientStamp);
                        // trustedPlayerComponent.Require<ServerStampComponent>().duration.Value += playerCommandsDuration;
                        // AnalysisLogger.AddDataPoint("", "A", trustedPlayerComponent.position.Value.x);
                        break;
                    }
                }
            });
            var localPlayerProperty = serverSession.Require<LocalPlayerProperty>();
            foreach (KeyValuePair<IPEndPoint,Container> pair in m_Socket.Connections)
            {
                byte playerId = pair.Value.Require<ByteProperty>();
                localPlayerProperty.Value = playerId;
                m_Socket.Send(serverSession, pair.Key);
            }
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}