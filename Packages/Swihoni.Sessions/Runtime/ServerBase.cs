using System;
using System.Collections.Generic;
using System.Net;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public abstract class ServerBase : NetworkedSessionBase
    {
        private ComponentServerSocket m_Socket;
        protected readonly DualDictionary<IPEndPoint, byte> m_PlayerIds = new DualDictionary<IPEndPoint, byte>();

        protected ServerBase(ISessionGameObjectLinker linker,
                             IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            ForEachPlayer(player => player.Add(typeof(ServerTag)));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentServerSocket(new IPEndPoint(IPAddress.Loopback, 7777));
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_EmptyClientCommands);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_EmptyServerSession);
            m_Socket.RegisterMessage(typeof(DebugClientView), m_EmptyDebugClientView);
        }

        protected virtual void PreTick(Container tickSession) { }

        protected virtual void PostTick(Container tickSession) { }

        protected sealed override void Tick(uint tick, float time, float duration)
        {
            Profiler.BeginSample("Server Setup");
            base.Tick(tick, time, duration);
            Container previousServerSession = m_SessionHistory.Peek(),
                      serverSession = m_SessionHistory.ClaimNext();
            serverSession.FastCopyFrom(previousServerSession);

            SessionSettingsComponent settings = GetSettings(serverSession);
            settings.FastCopyFrom(DebugBehavior.Singleton.Settings);
            Time.fixedDeltaTime = 1.0f / settings.tickRate;

            var serverStamp = serverSession.Require<ServerStampComponent>();
            serverStamp.tick.Value = tick;
            serverStamp.time.Value = time;
            serverStamp.duration.Value = duration;
            Profiler.EndSample();

            Profiler.BeginSample("Server Tick");
            PreTick(serverSession);
            Tick(serverSession);
            PostTick(serverSession);
            HandleTimeouts(time, serverSession);
            Profiler.EndSample();
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

        private void Tick(Container serverSession)
        {
            m_Socket.PollReceived((ipEndPoint, message) =>
            {
                (byte clientId, Container serverPlayer) = GetPlayerForEndpoint(serverSession, ipEndPoint);
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
                                serverPlayerClientStamp.FastMergeSet(clientStamp);
                            else
                            {
                                // Make sure this is the newest tick
                                if (clientStamp.tick > serverPlayerClientStamp.tick)
                                {
                                    serverPlayerTime.Value += clientStamp.time - serverPlayerClientStamp.time;

                                    if (Mathf.Abs(serverPlayerTime.Value - serverTime) > GetSettings(serverSession).TickInterval * 3)
                                    {
                                        // Debug.LogWarning($"[{GetType().Name}] Reset time for client: {clientId}");
                                        serverPlayerTime.Value = serverTime;
                                    }

                                    ModeBase mode = GetMode(serverSession);
                                    serverPlayer.FastMergeSet(clientCommands); // Merge in trusted
                                    m_Modifier[clientId].ModifyChecked(this, clientId, serverPlayer, clientCommands, clientStamp.duration);
                                    mode.Modify(serverPlayer, clientCommands, clientStamp.duration);
                                }
                                else
                                    Debug.LogWarning($"[{GetType().Name}] Received out of order command from client: {clientId}");
                            }
                        }
                        else
                            serverPlayerTime.Value = serverTime;
                        break;
                    }
                    case DebugClientView receivedDebugClientView:
                    {
                        PlayerVisualizerBehavior.Render(this, clientId, receivedDebugClientView, new Color(1.0f, 0.0f, 0.0f, 0.3f));
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

        /// <summary>
        /// Handles setting up player if new connection.
        /// </summary>
        /// <returns>Client id allocated to connection and player container</returns>
        private (byte clientId, Container serverPlayer) GetPlayerForEndpoint(Container serverSession, IPEndPoint ipEndPoint)
        {
            bool isNewPlayer = !m_PlayerIds.ContainsForward(ipEndPoint);
            if (isNewPlayer)
            {
                checked
                {
                    byte newPlayerId = 1;
                    while (m_PlayerIds.ContainsReverse(newPlayerId))
                        newPlayerId++;
                    m_PlayerIds.Add(new IPEndPoint(ipEndPoint.Address, ipEndPoint.Port), newPlayerId);
                    Debug.Log($"[{GetType().Name}] Received new connection: {ipEndPoint}, setting up id: {newPlayerId}");
                }
            }
            byte clientId = m_PlayerIds.GetForward(ipEndPoint);
            Container serverPlayer = serverSession.GetPlayer(clientId);
            if (isNewPlayer) SetupNewPlayer(serverSession, serverPlayer);
            return (clientId, serverPlayer);
        }

        protected void SetupNewPlayer(Container session, Container player)
        {
            GetMode(session).ResetPlayer(player);
            // TODO:refactor zeroing
            if (player.Has(out StatsComponent stats))
                stats.Zero();
            player.Require<ClientStampComponent>().Reset();
            player.Require<ServerStampComponent>().Reset();
        }

        public override Ray GetRayForPlayerId(int playerId)
        {
            return GetRayForPlayer(m_SessionHistory.Peek().GetPlayer(playerId));
        }

        protected override void RollbackHitboxes(int playerId)
        {
            for (var i = 0; i < m_Modifier.Length; i++)
            {
                int j = i;
                Container GetPlayerInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).GetPlayer(j);

                Container rollbackPlayer = m_RollbackSession.GetPlayer(i);

                FloatProperty time = GetPlayerInHistory(0).Require<ServerStampComponent>().time;
                if (!time.HasValue) continue;
                
                float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(GetSettings().TickInterval) * DebugBehavior.Singleton.HitboxRollbackOverride;
                RenderInterpolatedPlayer<ServerStampComponent>(time - rollback, rollbackPlayer,
                                                               m_SessionHistory.Size, GetPlayerInHistory);

                m_Modifier[i].EvaluateHitboxes(i, rollbackPlayer);
                if (i == 0) PlayerVisualizerBehavior.Render(this, i, rollbackPlayer, new Color(0.0f, 0.0f, 1.0f, 0.3f));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}