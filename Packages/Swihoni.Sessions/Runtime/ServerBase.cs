using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public abstract class ServerBase : NetworkedSessionBase
    {
        private readonly EventBasedNetListener.OnConnectionRequest m_ConnectionRequestHandler;
        private ComponentServerSocket m_Socket;
        private readonly Container m_SendSession;

        public override ComponentSocketBase Socket => m_Socket;

        protected ServerBase(SessionElements elements, IPEndPoint ipEndPoint, EventBasedNetListener.OnConnectionRequest connectionRequestHandler)
            : base(elements, ipEndPoint)
        {
            m_ConnectionRequestHandler = connectionRequestHandler;
            ForEachPlayer(player => player.RegisterAppend(typeof(ServerTag), typeof(ServerPingComponent)));
            m_SendSession = m_EmptyServerSession.Clone();
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(IpEndPoint, m_ConnectionRequestHandler);
            m_Socket.Listener.PeerDisconnectedEvent += OnPeerDisconnected;
            m_Socket.Listener.NetworkLatencyUpdateEvent += OnPingUpdate;
            RegisterMessages(m_Socket);

            Physics.autoSimulation = false;
        }

        private void OnPingUpdate(NetPeer peer, int latency)
        {
            Container player = GetPlayerFromId(peer.GetPlayerId());
            var ping = player.Require<ServerPingComponent>();
            ping.rtt.Value = latency / 1000.0f;
            if (player.With(out StatsComponent stats))
                stats.ping.Value = (ushort) (latency / 2);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnect)
        {
            int playerId = peer.GetPlayerId();
            Debug.LogWarning($"Dropping player with id: {playerId}, reason: {disconnect.Reason}, error code: {disconnect.SocketErrorCode}");
            GetPlayerFromId(playerId).Reset();
        }

        protected virtual void PreTick(Container tickSession) { }

        protected virtual void PostTick(Container tickSession) { }

        protected virtual void SettingsTick(Container serverSession)
        {
            var tickRate = serverSession.Require<TickRateProperty>();
            tickRate.CopyFrom(DebugBehavior.Singleton.TickRate);
            serverSession.Require<ModeIdProperty>().CopyFrom(DebugBehavior.Singleton.ModeId);
            Time.fixedDeltaTime = tickRate.TickInterval;
        }

        protected sealed override void Tick(uint tick, uint timeUs, uint durationUs)
        {
            base.Tick(tick, timeUs, durationUs);

            Profiler.BeginSample("Server Setup");
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            CopyFromPreviousSession(previousServerSession, serverSession);

            SettingsTick(serverSession);

            var serverStamp = serverSession.Require<ServerStampComponent>();
            serverStamp.tick.Value = tick;
            serverStamp.timeUs.Value = timeUs;
            serverStamp.durationUs.Value = durationUs;
            Profiler.EndSample();

            Profiler.BeginSample("Server Tick");
            PreTick(serverSession);
            Tick(serverSession, tick, timeUs, durationUs);
            PostTick(serverSession);
            // IterateClients(tick, time, duration, serverSession);
            Profiler.EndSample();
        }

        // private readonly List<byte> m_ToRemove = new List<byte>();
        //
        // private void IterateClients(uint tick, float time, float duration, Container serverSession)
        // {
        //     var players = serverSession.Require<PlayerContainerArrayElement>();
        //     m_ToRemove.Clear();
        //     foreach ((IPEndPoint _, byte playerId) in m_PlayerIds)
        //     {
        //         Container player = players[playerId];
        //         if (HandleTimeout(time, playerId, player)) m_ToRemove.Add(playerId);
        //         CheckClientPing(player, tick, time, playerId, duration);
        //     }
        //     foreach (byte playerId in m_ToRemove)
        //         m_PlayerIds.Remove(playerId);
        // }
        //
        // private void CheckClientPing(Container player, uint tick, float time, byte playerId, float duration)
        // {
        //     const float checkTime = 1.0f;
        //
        //     var ping = player.Require<ServerPingComponent>();
        //     if (ping.initiateTime.WithoutValue) return;
        //
        //     ping.tick.Value = tick;
        //     ping.initiateTime.Value = time;
        //     ping.checkElapsed.Value += duration;
        //     while (ping.checkElapsed > checkTime)
        //     {
        //         var check = new PingCheckComponent {tick = new UIntProperty(tick)};
        //         m_Socket.Send(check, playerId, DeliveryMethod.ReliableSequenced);
        //         ping.checkElapsed.Value -= checkTime;
        //     }
        // }
        // 
        // private static bool HandleTimeout(float time, byte playerId, Container player)
        // {
        //     FloatProperty serverPlayerTime = player.Require<ServerStampComponent>().time;
        //     if (serverPlayerTime.WithoutValue || Mathf.Abs(serverPlayerTime.Value - time) < 2.0f) return false;
        //     Debug.LogWarning($"Dropping player with id: {playerId}");
        //     player.Reset();
        //     return true;
        // }

        protected virtual void ServerTick(Container serverSession, uint timeUs, uint durationUs) { }

        private void Tick(Container serverSession, uint tick, uint timeUs, uint durationUs)
        {
            ServerTick(serverSession, timeUs, durationUs);
            m_Socket.PollReceived((fromPeer, element) =>
            {
                int clientId = fromPeer.GetPlayerId();
                Container serverPlayer = GetPlayerFromId(clientId);
                switch (element)
                {
                    case ClientCommandsContainer receivedClientCommands:
                    {
                        if (serverPlayer.Require<HealthProperty>().WithoutValue)
                        {
                            Debug.Log($"[{GetType().Name}] Setting up new player for connection: {fromPeer.EndPoint}, allocated id is: {fromPeer.GetPlayerId()}");
                            SetupNewPlayer(serverSession, serverPlayer);
                        }
                        HandleClientCommand(clientId, receivedClientCommands, serverSession, serverPlayer);
                        break;
                    }
                    // case PingCheckComponent receivedPingCheck:
                    // {
                    //     var ping = serverPlayer.Require<ServerPingComponent>();
                    //     if (receivedPingCheck.tick.WithValue && ping.tick == receivedPingCheck.tick)
                    //     {
                    //         float roundTripElapsed = time - ping.initiateTime;
                    //         ping.rtt.Value = roundTripElapsed;
                    //         if (serverPlayer.With(out StatsComponent stats))
                    //             stats.ping.Value = (ushort) Mathf.Round(roundTripElapsed / 2.0f * 1000.0f);
                    //     }
                    //     break;
                    // }
                    case DebugClientView receivedDebugClientView:
                    {
                        DebugBehavior.Singleton.Render(this, clientId, receivedDebugClientView, new Color(1.0f, 0.0f, 0.0f, 0.3f));
                        break;
                    }
                }
            });
            Physics.Simulate(durationUs * TimeConversions.MicrosecondToSecond);
            EntityManager.Modify(serverSession, timeUs, durationUs);
            GetMode(serverSession).Modify(serverSession, durationUs);
            SendServerSession(tick, serverSession);
        }

        private readonly List<NetPeer> m_ConnectedPeers = new List<NetPeer>();

        private void SendServerSession(uint tick, Container serverSession)
        {
            var localPlayerProperty = serverSession.Require<LocalPlayerProperty>();
            m_Socket.NetworkManager.GetPeersNonAlloc(m_ConnectedPeers, ConnectionState.Connected);
            foreach (NetPeer peer in m_ConnectedPeers)
            {
                int playerId = peer.GetPlayerId();
                localPlayerProperty.Value = (byte) playerId;
                Container player = GetPlayerFromId(playerId);
                uint lastServerTickAcknowledged = player.Require<AcknowledgedServerTickProperty>().OrElse(0u);
                var rollback = (int) checked(tick - lastServerTickAcknowledged);
                // if (lastServerTickAcknowledged == 0u)
                //     m_SendSession.CopyFrom(serverSession);
                // else
                // {
                //     // TODO:performance serialize and compress at the same time
                //     ElementExtensions.NavigateZipped(DeltaCompressNavigation, serverSession, m_SessionHistory.Get(-rollback), m_SendSession);
                // }
                m_SendSession.CopyFrom(serverSession);

                DeltaCompressAdditives(m_SendSession, rollback);
                m_Socket.Send(m_SendSession, peer, DeliveryMethod.ReliableUnordered);
            }
        }

        protected virtual void DeltaCompressAdditives(Container send, int rollback) { }

        private static Navigation DeltaCompressNavigation(ElementBase mostRecent, ElementBase lastAcknowledged, ElementBase send)
        {
            if (mostRecent is PropertyBase mostRecentProperty && lastAcknowledged is PropertyBase lastAcknowledgedProperty && send is PropertyBase sendProperty
             && !mostRecent.GetType().IsDefined(typeof(AdditiveAttribute)))
            {
                sendProperty.Clear();
                if (!mostRecentProperty.Equals(lastAcknowledgedProperty))
                    sendProperty.SetFromIfWith(mostRecentProperty);
            }
            return Navigation.Continue;
        }

        private void HandleClientCommand(int clientId, Container receivedClientCommands, Container serverSession, Container serverPlayer)
        {
            UIntProperty serverPlayerTimeUs = serverPlayer.Require<ServerStampComponent>().timeUs;
            var clientStamp = receivedClientCommands.Require<ClientStampComponent>();
            var serverStamp = serverSession.Require<ServerStampComponent>();
            var serverPlayerClientStamp = serverPlayer.Require<ClientStampComponent>();
            // Clients start to tag with ticks once they receive their first server player state
            if (clientStamp.tick.WithValue)
            {
                if (serverPlayerClientStamp.tick.WithoutValue)
                    // Take one tick to set initial server player client stamp
                    serverPlayerClientStamp.MergeFrom(clientStamp);
                else
                {
                    // Make sure this is the newest tick
                    if (clientStamp.tick > serverPlayerClientStamp.tick)
                    {
                        checked
                        {
                            serverPlayerTimeUs.Value += clientStamp.timeUs - serverPlayerClientStamp.timeUs;

                            long delta = serverPlayerTimeUs.Value - (long) serverStamp.timeUs;
                            if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3)
                            {
                                ResetErrors++;
                                serverPlayerTimeUs.Value = serverStamp.timeUs;
                            }
                        }

                        ModeBase mode = GetMode(serverSession);
                        serverPlayer.MergeFrom(receivedClientCommands); // Merge in trusted
                        m_Modifier[clientId].ModifyChecked(this, clientId, serverPlayer, receivedClientCommands, clientStamp.durationUs);
                        mode.Modify(serverSession, serverPlayer, receivedClientCommands, clientStamp.durationUs);
                    }
                    else Debug.LogWarning($"[{GetType().Name}] Received out of order command from client: {clientId}");
                }
            }
            else serverPlayerTimeUs.Value = serverStamp.timeUs;
        }

        // /// <summary>
        // /// Handles setting up player if new connection.
        // /// </summary>
        // /// <returns>Client id allocated to connection and player container</returns>
        // private (byte clientId, Container serverPlayer) GetPlayerForEndpoint(Container serverSession, IPEndPoint ipEndPoint)
        // {
        //     bool isNewPlayer = !m_PlayerIds.ContainsForward(ipEndPoint);
        //     if (isNewPlayer)
        //     {
        //         checked
        //         {
        //             byte newPlayerId = 1;
        //             while (m_PlayerIds.ContainsReverse(newPlayerId))
        //                 newPlayerId++;
        //             m_PlayerIds.Add(new IPEndPoint(ipEndPoint.Address, ipEndPoint.Port), newPlayerId);
        //             Debug.Log($"[{GetType().Name}] Received new connection: {ipEndPoint}, setting up id: {newPlayerId}");
        //         }
        //     }
        //     byte clientId = m_PlayerIds.GetForward(ipEndPoint);
        //     Container serverPlayer = serverSession.GetPlayer(clientId);
        //     if (isNewPlayer) SetupNewPlayer(serverSession, serverPlayer);
        //     return (clientId, serverPlayer);
        // }

        protected void SetupNewPlayer(Container session, Container player)
        {
            GetMode(session).SpawnPlayer(player);
            // TODO:refactor zeroing

            player.ZeroIfWith<StatsComponent>();
            player.Require<ServerPingComponent>().Zero();
            player.Require<ClientStampComponent>().Reset();
            player.Require<ServerStampComponent>().Reset();
        }

        public override Ray GetRayForPlayerId(int playerId) => GetRayForPlayer(GetLatestSession().GetPlayer(playerId));

        protected override void RollbackHitboxes(int playerId)
        {
            float rtt = GetPlayerFromId(playerId).Require<ServerPingComponent>().rtt;
            for (var _ = 0; _ < m_Modifier.Length; _++)
            {
                int modifierId = _;
                Container GetPlayerInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).GetPlayer(modifierId);

                Container rollbackPlayer = m_RollbackSession.GetPlayer(modifierId);

                UIntProperty timeUs = GetPlayerInHistory(0).Require<ServerStampComponent>().timeUs;
                if (timeUs.WithoutValue) continue;

                /* See: https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking */
                float tickInterval = DebugBehavior.Singleton.RollbackOverride.OrElse(GetLatestSession().Require<TickRateProperty>().TickInterval);
                uint rollbackUs = TimeConversions.GetUsFromSecond(tickInterval * 3.6f + rtt);
                RenderInterpolatedPlayer<ServerStampComponent>(timeUs - rollbackUs, rollbackPlayer,
                                                               m_SessionHistory.Size, GetPlayerInHistory);

                m_Modifier[modifierId].EvaluateHitboxes(modifierId, rollbackPlayer);
                if (modifierId == 0)
                    DebugBehavior.Singleton.Render(this, modifierId, rollbackPlayer, new Color(0.0f, 0.0f, 1.0f, 0.3f));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}