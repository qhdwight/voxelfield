using System;
using System.Collections.Generic;
using System.Net;
using Swihoni.Networking;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions
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

    public abstract class ServerBase : NetworkedSessionBase
    {
        private ComponentServerSocket m_Socket;

        protected ServerBase(IGameObjectLinker linker,
                             IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            // foreach (ServerSessionContainer serverSession in m_SessionHistory)
            // foreach (Container player in serverSession.Require<PlayerContainerArrayProperty>())
            //     player.Require<ClientStampComponent>().time.DoSerialization = false;
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

        protected sealed override void Tick(uint tick, float time, float duration)
        {
            base.Tick(tick, time, duration);
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            serverSession.CopyFrom(previousServerSession);
            if (serverSession.If(out ServerStampComponent serverStamp))
            {
                serverStamp.tick.Value = tick;
                serverStamp.time.Value = time;
                serverStamp.duration.Value = duration;

                PreTick(serverSession);
                Tick(previousServerSession, serverSession);
                PostTick(serverSession);
            }
        }

        private void Tick(Container previousServerSession, Container serverSession)
        {
            var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
            var serverStamp = serverSession.Require<ServerStampComponent>();
            foreach (Container serverPlayer in serverPlayers)
            {
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
                        // Make sure this is the newest tick
                        var previousClientStamp = previousServerSession.Require<PlayerContainerArrayProperty>()[clientId].Require<ClientStampComponent>();
                        if (clientStamp.tick <= previousClientStamp.tick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order client command");
                            break;
                        }
                        Container serverPlayer = serverPlayers[clientId];
                        var serverPlayerStamp = serverPlayer.Require<ServerStampComponent>();
                        if (serverPlayerStamp.time.HasValue)
                            serverPlayerStamp.time.Value += clientStamp.time - previousClientStamp.time;
                        else
                            serverPlayerStamp.time.Value = serverStamp.time;

                        serverPlayer.Require<ClientStampComponent>().duration.Reset();
                        serverPlayer.MergeSet(clientCommands);
                        m_Modifier[clientId].ModifyChecked(serverPlayer, clientCommands, clientStamp.duration);

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