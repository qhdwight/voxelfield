using System;
using System.Collections.Generic;
using System.Net;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Networking;
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
        public ServerSessionContainer() { }
        public ServerSessionContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable]
    public class ServerStampComponent : StampComponent
    {
    }

    public abstract class ServerBase : NetworkedSessionBase
    {
        private ComponentServerSocket m_Socket;
        protected readonly DualDictionary<IPEndPoint, byte> m_PlayerIds = new DualDictionary<IPEndPoint, byte>();

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

        protected virtual void PreTick(Container tickSession) { }

        protected virtual void PostTick(Container tickSession) { }

        protected sealed override void Tick(uint tick, float time, float duration)
        {
            base.Tick(tick, time, duration);
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            serverSession.CopyFrom(previousServerSession);
            if (serverSession.Has(out ServerStampComponent serverStamp))
            {
                serverStamp.tick.Value = tick;
                serverStamp.time.Value = time;
                serverStamp.duration.Value = duration;

                PreTick(serverSession);
                Tick(serverSession);
                PostTick(serverSession);

                HandleTimeouts(time, serverSession);
            }
        }

        private void HandleTimeouts(float time, Container serverSession)
        {
            var players = serverSession.Require<PlayerContainerArrayProperty>();
            for (byte playerId = 1; playerId < players.Length; playerId++)
            {
                Container player = players[playerId];
                FloatProperty serverPlayerTime = player.Require<ServerStampComponent>().time;
                if (!serverPlayerTime.HasValue || Mathf.Abs(serverPlayerTime.Value - time) < 2.0f) continue;
                Debug.LogWarning($"Dropping player with id: {playerId}");
                m_PlayerIds.Remove(playerId);
                player.Reset();
            }
        }

        protected static void NewPlayer(Container player)
        {
            // TODO:refactor
            player.Zero();
            player.Require<ClientStampComponent>().Reset();
            player.Require<ServerStampComponent>().Reset();
            if (player.Has(out HealthProperty healthProperty))
                healthProperty.Value = 100;
            if (player.Has(out InventoryComponent inventoryComponent))
            {
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventoryComponent, ItemId.TestingRifle, 2);
            }
        }

        private void Tick(Container serverSession)
        {
            m_Socket.PollReceived((ipEndPoint, message) =>
            {
                bool isNewPlayer = !m_PlayerIds.ContainsForward(ipEndPoint);
                if (isNewPlayer)
                {
                    checked
                    {
                        byte newPlayerId = 1;
                        while (m_PlayerIds.ContainsReverse(newPlayerId))
                        {
                            newPlayerId++;
                        }
                        m_PlayerIds.Add(new IPEndPoint(ipEndPoint.Address, ipEndPoint.Port), newPlayerId);
                        Debug.Log($"[{GetType().Name}] Received new connection: {ipEndPoint}, setting up id: {newPlayerId}");
                    }
                }
                byte clientId = m_PlayerIds.GetForward(ipEndPoint);
                Container serverPlayer = serverSession.GetPlayer(clientId);
                if (isNewPlayer) NewPlayer(serverPlayer);
                switch (message)
                {
                    case ClientCommandsContainer clientCommands:
                    {
                        FloatProperty serverPlayerTime = serverPlayer.Require<ServerStampComponent>().time;
                        var clientStamp = clientCommands.Require<ClientStampComponent>();
                        float serverTime = serverSession.Require<ServerStampComponent>().time;
                        var serverPlayerClientStamp = serverPlayer.Require<ClientStampComponent>();
                        // Clients start to tag with ticks once they receive their first server player state
                        if (clientStamp.tick.HasValue)
                        {
                            if (!serverPlayerClientStamp.tick.HasValue)
                                // Take one tick to set initial server player client stamp
                                serverPlayerClientStamp.MergeSet(clientStamp);
                            else
                            {
                                // Make sure this is the newest tick
                                if (clientStamp.tick > serverPlayerClientStamp.tick)
                                {
                                    serverPlayerTime.Value += clientStamp.time - serverPlayerClientStamp.time;

                                    if (Mathf.Abs(serverPlayerTime.Value - serverTime) > m_Settings.TickInterval)
                                    {
                                        Debug.LogWarning($"[{GetType().Name}] Reset time for client: {clientId}");
                                        serverPlayerTime.Value = serverTime;
                                    }

                                    serverPlayer.MergeSet(clientCommands);
                                    m_Modifier[clientId].ModifyChecked(serverPlayer, clientCommands, clientStamp.duration);
                                }
                                else
                                {
                                    Debug.LogWarning($"[{GetType().Name}] Received out of order command from client: {clientId}");
                                }
                            }
                        }
                        else
                        {
                            serverPlayerTime.Value = serverTime;
                        }
                        break;
                    }
                }
            });
            var localPlayerProperty = serverSession.Require<LocalPlayerProperty>();
            foreach ((IPEndPoint ipEndPoint, byte id) in m_PlayerIds)
            {
                localPlayerProperty.Value = id;
                m_Socket.Send(serverSession, ipEndPoint);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}