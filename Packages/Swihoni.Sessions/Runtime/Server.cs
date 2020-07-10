using System;
using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace Swihoni.Sessions
{
    public class Server : NetworkedSessionBase
    {
        private ComponentServerSocket m_Socket;
        private readonly Container m_SendSession;

        public override ComponentSocketBase Socket => m_Socket;

        public Server(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector)
            : base(elements, ipEndPoint, injector)
        {
            ForEachPlayer(player => player.RegisterAppend(typeof(ServerTag), typeof(ServerPingComponent), typeof(HasSentInitialData)));
            m_SendSession = m_EmptyServerSession.Clone();
        }

        public override void Start()
        {
            base.Start();
            Random.InitState(Environment.TickCount);
            m_Socket = new ComponentServerSocket(IpEndPoint, m_Injector.OnHandleNewConnection);
            m_Socket.Listener.PeerDisconnectedEvent += OnPeerDisconnected;
            m_Socket.Listener.NetworkLatencyUpdateEvent += OnLatencyUpdated;
            m_Socket.OnReceive = OnReceive;
            RegisterMessages(m_Socket);

            Physics.autoSimulation = false;
        }

        private void OnLatencyUpdated(NetPeer peer, int latency)
        {
            Container player = GetPlayerFromId(peer.GetPlayerId());
            var ping = player.Require<ServerPingComponent>();
            ping.latencyUs.Value = checked((uint) latency * 1_000);
            if (player.With(out StatsComponent stats))
                stats.ping.Value = checked((ushort) (latency / 2));
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnect)
        {
            int playerId = peer.GetPlayerId();
            Debug.LogWarning($"Dropping player with id: {playerId}, reason: {disconnect.Reason}, error code: {disconnect.SocketErrorCode}");
            Container player = GetPlayerFromId(playerId);
            player.Clear();
            GetPlayerModifier(player, playerId);
        }

        protected virtual void PreTick(Container tickSession) { }

        protected virtual void PostTick(Container tickSession) { }

        protected override void Render(uint renderTimeUs) { }

        protected sealed override void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Profiler.BeginSample("Server Setup");
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            CopyFromPreviousSession(previousServerSession, serverSession);

            base.Tick(tick, timeUs, durationUs);

            m_Injector.OnSettingsTick(serverSession);

            var serverStamp = serverSession.Require<ServerStampComponent>();
            serverStamp.tick.Value = tick;
            serverStamp.timeUs.Value = timeUs;
            serverStamp.durationUs.Value = durationUs;
            Profiler.EndSample();

            Profiler.BeginSample("Server Tick");
            PreTick(serverSession);
            Tick(serverSession, tick, timeUs, durationUs); // Send
            PostTick(serverSession);
            // IterateClients(tick, time, duration, serverSession);
            Profiler.EndSample();

            ElementExtensions.NavigateZipped((_previous, _current) =>
            {
                if (_current.WithAttribute<SingleTick>())
                    _current.Clear();
                if (_current is PropertyBase _currentProperty)
                    _currentProperty.IsOverride = false;
                return Navigation.Continue;
            }, previousServerSession, serverSession);
        }

        private static void CopyFromPreviousSession(ElementBase previous, ElementBase current)
        {
            ElementExtensions.NavigateZipped((_previous, _current) =>
            {
                if (_previous is PropertyBase _previousProperty && _current is PropertyBase _currentProperty)
                {
                    _currentProperty.SetTo(_previousProperty);
                    _currentProperty.IsOverride = _previousProperty.IsOverride;
                }
                return Navigation.Continue;
            }, previous, current);
        }

        protected virtual void ServerTick(Container serverSession, uint timeUs, uint durationUs) { }

        private void OnReceive(NetPeer fromPeer, ElementBase element)
        {
            Container serverSession = GetLatestSession();
            int clientId = fromPeer.GetPlayerId();
            Container serverPlayer = GetPlayerFromId(clientId);
            switch (element)
            {
                case ClientCommandsContainer receivedClientCommands:
                {
                    if (serverPlayer.Require<HealthProperty>().WithoutValue)
                    {
                        Debug.Log($"[{GetType().Name}] Setting up new player for connection: {fromPeer.EndPoint}, allocated id is: {fromPeer.GetPlayerId()}");
                        SetupNewPlayer(serverSession, clientId, serverPlayer);
                    }
                    HandleClientCommand(clientId, receivedClientCommands, serverSession, serverPlayer);
                    break;
                }
                case DebugClientView receivedDebugClientView:
                {
                    DebugBehavior.Singleton.Render(this, clientId, receivedDebugClientView, new Color(1.0f, 0.0f, 0.0f, 0.3f));
                    break;
                }
            }
        }

        private void Tick(Container serverSession, uint tick, uint timeUs, uint durationUs)
        {
            ServerTick(serverSession, timeUs, durationUs);
            m_Socket.PollEvents();
            Physics.Simulate(durationUs * TimeConversions.MicrosecondToSecond);

            void IterateEntity(ModifierBehaviorBase modifer, int _, Container entity) => ((EntityModifierBehavior) modifer).Modify(this, entity, timeUs, durationUs);
            EntityManager.ModifyAll(serverSession, IterateEntity);
            GetMode(serverSession).Modify(this, serverSession, durationUs);

            SendServerSession(tick, serverSession);
        }

        private readonly List<NetPeer> m_ConnectedPeers = new List<NetPeer>();

        private void SendServerSession(uint tick, Container serverSession)
        {
            m_Socket.NetworkManager.GetPeersNonAlloc(m_ConnectedPeers, ConnectionState.Connected);
            foreach (NetPeer peer in m_ConnectedPeers)
                SendPeerLatestSession(tick, peer, serverSession);
        }

        protected void SendPeerLatestSession(uint tick, NetPeer peer, Container serverSession)
        {
            Container player = GetPlayerFromId(peer.GetPlayerId(), serverSession);

            if (player.Require<HealthProperty>().WithValue)
            {
                var localPlayerProperty = serverSession.Require<LocalPlayerId>();
                int playerId = peer.GetPlayerId();
                localPlayerProperty.Value = (byte) playerId;

                uint lastServerTickAcknowledged = player.Require<AcknowledgedServerTickProperty>().Else(0u);
                var rollback = checked((int) (tick - lastServerTickAcknowledged));
                if (lastServerTickAcknowledged == 0u)
                    // m_SendSession.CopyFrom(serverSession);
                    CopyToSend(serverSession);
                else
                    // TODO:performance serialize and compress at the same time
                    CompressSession(serverSession, rollback);

                // CopyToSend(serverSession);

                if (player.Require<ClientStampComponent>().tick.WithValue)
                {
                    BoolProperty hasSentInitialData = player.Require<HasSentInitialData>();
                    if (!hasSentInitialData)
                    {
                        m_Injector.OnSendInitialData(peer, serverSession, m_SendSession);
                        hasSentInitialData.Value = true;
                    }
                }

                m_Socket.Send(m_SendSession, peer, DeliveryMethod.ReliableUnordered);
            }
        }

        private void CopyToSend(ElementBase serverSession)
        {
            ElementExtensions.NavigateZipped((_send, _server) =>
            {
                if (_send is PropertyBase _sendProperty && _server is PropertyBase _serverProperty)
                {
                    _sendProperty.SetTo(_serverProperty);
                    _sendProperty.IsOverride = _serverProperty.IsOverride;
                }
                return Navigation.Continue;
            }, m_SendSession, serverSession);
        }

        // Not working, prediction errors on ground tick and position
        private void CompressSession(ElementBase serverSession, int rollback)
        {
            ElementExtensions.NavigateZipped((_mostRecent, _lastAcknowledged, _send) =>
            {
                if (_mostRecent is PropertyBase _mostRecentProperty && _lastAcknowledged is PropertyBase _lastAcknowledgedProperty && _send is PropertyBase _sendProperty)
                {
                    if (_mostRecent.WithoutAttribute<SingleTick>()
                     && _mostRecent.WithoutAttribute<NeverCompress>()
                     && !(_mostRecentProperty is VectorProperty)
                     && !(_mostRecentProperty is StringProperty)
                     && _mostRecentProperty.Equals(_lastAcknowledgedProperty))
                    {
                        _sendProperty.Clear();
                        _sendProperty.WasSame = true;
                    }
                    else
                    {
                        _sendProperty.SetTo(_mostRecentProperty);
                        _sendProperty.IsOverride = _mostRecentProperty.IsOverride;
                        _sendProperty.WasSame = false;
                    }
                }
                return Navigation.Continue;
            }, serverSession, m_SessionHistory.Get(-1), m_SendSession);
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
                    var tickDelta = checked((int) (clientStamp.tick - (long) serverPlayerClientStamp.tick));
                    bool isLatestTick = tickDelta >= 1;
                    ModeBase mode = GetMode(serverSession);
                    if (isLatestTick)
                    {
                        checked
                        {
                            serverPlayerTimeUs.Value += clientStamp.timeUs - serverPlayerClientStamp.timeUs;

                            long deltaUs = serverPlayerTimeUs.Value - (long) serverStamp.timeUs;
                            if (Math.Abs(deltaUs) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                            {
                                ResetErrors++;
                                serverPlayerTimeUs.Value = serverStamp.timeUs;
                            }
                        }
                        if (!IsPaused)
                        {
                            MergeTrustedFromCommands(serverPlayer, receivedClientCommands);
                            // serverPlayer.MergeFrom(receivedClientCommands);
                        }
                    }
                    else Debug.LogWarning($"[{GetType().Name}] Received out of order command from client: {clientId}");

                    if (!IsPaused)
                    {
                        GetPlayerModifier(serverPlayer, clientId).ModifyChecked(this, clientId, serverPlayer, receivedClientCommands, clientStamp.durationUs, tickDelta);
                        mode.ModifyPlayer(this, serverSession, clientId, serverPlayer, receivedClientCommands, clientStamp.durationUs, tickDelta);
                    }
                }
            }
            else
            {
                serverPlayerTimeUs.Value = serverStamp.timeUs;
            }
        }

        private static void MergeTrustedFromCommands(ElementBase serverPlayer, ElementBase receivedClientCommands)
        {
            ElementExtensions.NavigateZipped((_server, _client) =>
            {
                if (_client.WithAttribute<ClientTrustedAttribute>())
                {
                    _server.CopyFrom(_client);
                    return Navigation.SkipDescendents;
                }
                return Navigation.Continue;
            }, serverPlayer, receivedClientCommands);
        }

        protected void SetupNewPlayer(Container session, int playerId, Container player)
        {
            GetMode(session).SetupNewPlayer(this, playerId, player);
            // TODO:refactor zeroing

            player.ZeroIfWith<StatsComponent>();
            player.Require<HasSentInitialData>().Zero();
            player.Require<ServerPingComponent>().Zero();
            player.Require<ClientStampComponent>().Clear();
            player.Require<ServerStampComponent>().Clear();
        }

        public override Ray GetRayForPlayerId(int playerId) => GetRayForPlayer(GetLatestSession().GetPlayer(playerId));

        protected override void RollbackHitboxes(int playerId)
        {
            uint latencyUs = GetPlayerFromId(playerId).Require<ServerPingComponent>().latencyUs;
            for (var _modifierId = 0; _modifierId < MaxPlayers; _modifierId++)
            {
                int modifierId = _modifierId; // Copy for use in lambda
                Container GetPlayerInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).GetPlayer(modifierId);

                Container rollbackPlayer = m_RollbackSession.GetPlayer(modifierId);
                // UIntProperty timeUs = GetPlayerInHistory(0).Require<ServerStampComponent>().timeUs;
                UIntProperty timeUs = GetLatestSession().Require<ServerStampComponent>().timeUs;
                if (timeUs.WithoutValue) continue;

                checked
                {
                    /* See: https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking */
                    uint tickIntervalUs = DebugBehavior.Singleton.RollbackOverrideUs.Else(GetLatestSession().Require<TickRateProperty>().PlayerRenderIntervalUs * 2),
                         rollbackUs = tickIntervalUs + latencyUs;
                    RenderInterpolatedPlayer<ServerStampComponent>(timeUs - rollbackUs, rollbackPlayer,
                                                                   m_SessionHistory.Size, GetPlayerInHistory);
                }
                PlayerModifierDispatcherBehavior modifier = GetPlayerModifier(rollbackPlayer, modifierId);
                if (modifier) modifier.EvaluateHitboxes(this, modifierId, rollbackPlayer);

                if (modifierId == 0) DebugBehavior.Singleton.Render(this, modifierId, rollbackPlayer, new Color(0.0f, 0.0f, 1.0f, 0.3f));
            }
        }

        public override void StringCommand(int playerId, string stringCommand)
            => GetPlayerFromId(playerId).Require<StringCommandProperty>().SetTo(stringCommand);

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}