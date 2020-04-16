using System;
using System.Collections.Generic;
using System.Net;
using Components;
using Networking;
using Session.Components;
using Session.Items.Modifiers;
using Session.Player.Components;
using Session.Player.Modifiers;
using UnityEngine;

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
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_EmptyClientCommands);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_EmptyServerSession);
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
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            serverSession.CopyFrom(previousServerSession);
            if (serverSession.If(out ServerStampComponent serverStamp))
            {
                serverStamp.tick.Value = tick;
                serverStamp.time.Value = time;
                float duration = time - previousServerSession.Require<ServerStampComponent>().time.OrElse(time);
                serverStamp.duration.Value = duration;

                var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
                foreach (Container serverPlayer in serverPlayers)
                {
                    if (serverPlayer.If(out ClientStampComponent clientStamp))
                        clientStamp.duration.Value = 0u;
                    if (serverPlayer.If(out ServerStampComponent serverPlayerStamp))
                        serverPlayerStamp.CopyFrom(serverStamp);
                }
                
                PreTick(serverSession);
                Tick(previousServerSession, serverSession);
                PostTick(serverSession);
            }
        }

        private void Tick(Container previousServerSession, Container serverSession)
        {
            var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
            foreach (Container serverPlayer in serverPlayers)
            {
                var serverStamp = serverSession.Require<ServerStampComponent>();
                if (serverStamp.tick == 0u)
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
                        var clientStamp = clientCommands.Require<ClientStampComponent>();
                        // Make sure this is newer tick
                        uint previousClientTick = previousServerSession.Require<PlayerContainerArrayProperty>()[clientId].Require<ClientStampComponent>().tick;
                        if (clientStamp.tick <= previousClientTick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order client command");
                            break;
                        }
                        
                        Container serverPlayer = serverPlayers[clientId];
                        serverPlayer.MergeSet(clientCommands);
                        m_Modifier[clientId].ModifyChecked(serverPlayer, clientCommands, clientStamp.duration);
                        var serverClientStamp = serverPlayer.Require<ClientStampComponent>();
                        serverClientStamp.tick.Value = clientStamp.tick;
                        serverClientStamp.time.Value = clientStamp.time;
                        serverClientStamp.duration.Value += clientStamp.duration;
                        break;
                    }
                }
            });
            var localPlayerProperty = serverSession.Require<LocalPlayerProperty>();
            foreach (KeyValuePair<IPEndPoint, Container> pair in m_Socket.Connections)
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