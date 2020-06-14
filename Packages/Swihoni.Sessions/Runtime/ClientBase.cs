using System;
using System.Linq;
using System.Net;
using System.Reflection;
using LiteNetLib;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public abstract class ClientBase : NetworkedSessionBase
    {
        private readonly string m_ConnectKey;
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;
        private float? m_ServerReceiveTime;

        public int PredictionErrors { get; private set; }
        public override ComponentSocketBase Socket => m_Socket;

        protected ClientBase(SessionElements elements, IPEndPoint ipEndPoint, string connectKey)
            : base(elements, ipEndPoint)
        {
            m_ConnectKey = connectKey;
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(250, () => m_EmptyClientCommands.Clone());
            // TODO:refactor zeroing
            ZeroCommand(m_CommandHistory.Peek());
            m_PlayerPredictionHistory = new CyclicArray<Container>(250, () => new Container(elements.playerElements.Append(typeof(ClientStampComponent))));
            m_PlayerPredictionHistory.Peek().Zero();
            m_PlayerPredictionHistory.Peek().Require<ClientStampComponent>().Reset();

            foreach (ServerSessionContainer session in m_SessionHistory)
            {
                session.RegisterAppend(typeof(LocalizedClientStampComponent));
                foreach (Container player in session.Require<PlayerContainerArrayElement>())
                    player.RegisterAppend(typeof(LocalizedClientStampComponent));
            }
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(IpEndPoint, m_ConnectKey);
            m_Socket.Listener.PeerDisconnectedEvent += OnDisconnect;
            RegisterMessages(m_Socket);
        }

        private void OnDisconnect(NetPeer peer, DisconnectInfo disconnect)
        {
            if (disconnect.AdditionalData.TryGetString(out string reason))
                Debug.Log($"Disconnected for reason: {reason}");
            Dispose();
        }

        private void UpdateInputs(int localPlayerId) => m_Modifier[localPlayerId].ModifyCommands(this, m_CommandHistory.Peek());

        protected override void Input(float time, float delta)
        {
            if (!GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
                return;

            UpdateInputs(localPlayerId);
            m_Modifier[localPlayerId].ModifyTrusted(this, localPlayerId, m_CommandHistory.Peek(), m_CommandHistory.Peek(), delta);
        }

        protected override void Render(float renderTime)
        {
            if (m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers) || !GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
                return;

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (tickRate.WithoutValue) return;

            base.Render(renderTime);

            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isLocalPlayer = playerId == localPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                if (isLocalPlayer)
                {
                    Container GetInHistory(int historyIndex) => m_PlayerPredictionHistory.Get(-historyIndex);
                    float rollback = tickRate.TickInterval;
                    RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayer, m_PlayerPredictionHistory.Size, GetInHistory);
                    renderPlayer.MergeFrom(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[copiedPlayerId];

                    float rollback = tickRate.TickInterval * 4;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(renderTime - rollback, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayer, isLocalPlayer);
                m_PlayerHud.Render(renderPlayers[localPlayerId]);
            }
            RenderEntities<LocalizedClientStampComponent>(renderTime, tickRate.TickInterval * 2);
        }

        protected override void Tick(uint tick, float time, float duration)
        {
            base.Tick(tick, time, duration);

            Profiler.BeginSample("Client Predict");
            Container latestSession = GetLatestSession();
            if (GetLocalPlayerId(latestSession, out int localPlayerId))
            {
                SettingsTick(latestSession);
                UpdateInputs(localPlayerId);
                Predict(tick, time, localPlayerId);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Client Send");
            Send();
            Profiler.EndSample();

            Profiler.BeginSample("Client Receive");
            Receive(time);
            // HandleTimeouts(time);
            Profiler.EndSample();
        }

        protected virtual void SettingsTick(Container serverSession) { }

        // private void HandleTimeouts(float time)
        // {
        //     if (!m_ServerReceiveTime.HasValue || Mathf.Abs(m_ServerReceiveTime.Value - time) < 2.0f) return;
        //
        //     Debug.LogWarning($"[{GetType().Name}] Disconnected due to stale connection!");
        //     Dispose();
        // }

        private void Predict(uint tick, float time, int localPlayerId)
        {
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.Without(out ClientStampComponent predictedStamp)) return;

            predictedPlayer.CopyFrom(previousPredictedPlayer);
            commands.CopyFrom(previousCommand);

            if (IsPaused)
            {
                commands.Require<ClientStampComponent>().Reset();
            }
            else
            {
                predictedStamp.tick.Value = tick;
                predictedStamp.time.Value = time;
                var previousClientStamp = previousPredictedPlayer.Require<ClientStampComponent>();
                if (previousClientStamp.time.WithValue)
                {
                    float lastTime = previousClientStamp.time.OrElse(time),
                          duration = time - lastTime;
                    predictedStamp.duration.Value = duration;
                }

                // Inject trusted component
                commands.Require<ClientStampComponent>().CopyFrom(predictedStamp);
                predictedPlayer.MergeFrom(commands);
                if (predictedStamp.duration.WithValue)
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, predictedPlayer, commands, predictedStamp.duration);
            }
        }

        private void Send() => m_Socket.SendToServer(m_CommandHistory.Peek(), DeliveryMethod.ReliableUnordered);

        private void CheckPrediction(Container serverSession)
        {
            if (!GetLocalPlayerId(serverSession, out int localPlayerId))
                return;

            Container serverPlayer = serverSession.GetPlayer(localPlayerId);
            UIntProperty targetTick = serverPlayer.Require<ClientStampComponent>().tick;

            if (targetTick.WithoutValue)
                return;
            for (var playerHistoryIndex = 0; playerHistoryIndex < m_PlayerPredictionHistory.Size; playerHistoryIndex++)
            {
                Container predictedPlayer = m_PlayerPredictionHistory.Get(-playerHistoryIndex);
                if (predictedPlayer.Require<ClientStampComponent>().tick != targetTick) continue;
                /* We are checking predicted */
                var areEqual = true;
                Container latestPredictedPlayer = m_PlayerPredictionHistory.Peek();
                ElementExtensions.NavigateZipped((predictedElement, latestPredictedElement, serverElement) =>
                {
                    Type type = predictedElement.GetType();
                    if (type.IsDefined(typeof(OnlyServerTrustedAttribute)))
                    {
                        latestPredictedElement.MergeFrom(serverElement);
                        return Navigation.SkipDescendents;
                    }
                    if (type.IsDefined(typeof(ClientTrustedAttribute)))
                        return Navigation.SkipDescendents;
                    switch (predictedElement)
                    {
                        case PropertyBase p1 when serverElement is PropertyBase p2 && !p1.Equals(p2):
                            areEqual = false;
                            Debug.LogWarning($"Prediction error with {p1.GetType().Name} with predicted: {p1} and verified: {p2}");
                            return Navigation.Exit;
                    }
                    return Navigation.Continue;
                }, predictedPlayer, latestPredictedPlayer, serverPlayer);
                if (areEqual) break;
                /* We did not predict properly */
                PredictionErrors++;
                // Place base from verified server
                predictedPlayer.CopyFrom(serverPlayer);
                // Replay old commands up until most recent to get back on track
                for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
                {
                    ClientCommandsContainer commands = m_CommandHistory.Get(-commandHistoryIndex);
                    Container pastPredictedPlayer = m_PlayerPredictionHistory.Get(-commandHistoryIndex);
                    ClientStampComponent stamp = pastPredictedPlayer.Require<ClientStampComponent>().Clone(); // TODO:performance remove clone
                    pastPredictedPlayer.CopyFrom(m_PlayerPredictionHistory.Get(-commandHistoryIndex - 1));
                    pastPredictedPlayer.Require<ClientStampComponent>().CopyFrom(stamp);
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, pastPredictedPlayer, commands, commands.Require<ClientStampComponent>().duration);
                }
                break;
            }
        }

        private void Receive(float time)
        {
            m_Socket.PollReceived((peer, message) =>
            {
                m_ServerReceiveTime = time;
                switch (message)
                {
                    case ServerSessionContainer receivedServerSession:
                    {
                        Profiler.BeginSample("Client Receive Setup");
                        ServerSessionContainer previousServerSession = m_SessionHistory.Peek(),
                                               serverSession = m_SessionHistory.ClaimNext();
                        CopyFromPreviousSession(previousServerSession, serverSession);
                        serverSession.MergeFrom(receivedServerSession);
                        Profiler.EndSample();

                        Received(serverSession);

                        uint serverTick = serverSession.Require<ServerStampComponent>().tick;
                        UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                        if (previousServerTick.WithValue && serverTick <= previousServerTick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            break;
                        }
                        if (previousServerTick.WithValue)
                        {
                            uint delta = serverTick - previousServerTick;
                            m_CommandHistory.Peek().Require<AcknowledgedServerTickProperty>().Value = serverTick - delta + 1;
                        }

                        {
                            // TODO:refactor make function
                            FloatProperty serverTime = serverSession.Require<ServerStampComponent>().time,
                                          localizedServerTime = serverSession.Require<LocalizedClientStampComponent>().time;

                            if (localizedServerTime.WithValue)
                                localizedServerTime.Value += serverTime - previousServerSession.Require<ServerStampComponent>().time;
                            else
                                localizedServerTime.Value = time;

                            if (Mathf.Abs(localizedServerTime.Value - time) > serverSession.Require<TickRateProperty>().TickInterval * 3)
                            {
                                ResetErrors++;
                                localizedServerTime.Value = time;
                            }
                        }

                        Profiler.BeginSample("Client Update Players");
                        var serverPlayers = serverSession.Require<PlayerContainerArrayElement>();
                        for (var playerId = 0; playerId < serverPlayers.Length; playerId++)
                        {
                            Container serverPlayer = serverPlayers[playerId];
                            var healthProperty = serverPlayer.Require<HealthProperty>();
                            if (healthProperty.WithoutValue || healthProperty.IsDead) continue;
                            /* We have been acknowledged by the server */

                            FloatProperty serverTime = serverPlayer.Require<ServerStampComponent>().time,
                                          localizedServerTime = serverPlayer.Require<LocalizedClientStampComponent>().time;

                            if (localizedServerTime.WithValue)
                                localizedServerTime.Value += serverTime - previousServerSession.GetPlayer(playerId).Require<ServerStampComponent>().time;
                            else
                                localizedServerTime.Value = time;

                            GetLocalPlayerId(serverSession, out int localPlayerId);
                            if (playerId != localPlayerId) m_Modifier[playerId].Synchronize(serverPlayer);

                            if (Mathf.Abs(localizedServerTime.Value - time) > serverSession.Require<TickRateProperty>().TickInterval * 3)
                            {
                                // Debug.LogWarning($"[{GetType().Name}] Client reset");
                                ResetErrors++;
                                localizedServerTime.Value = time;
                            }
                        }
                        Profiler.EndSample();

                        // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                        Profiler.BeginSample("Client Check Prediction");
                        CheckPrediction(serverSession);
                        Profiler.EndSample();

                        break;
                    }
                    // case PingCheckComponent receivedPingCheck:
                    // {
                    //     m_Socket.SendToServer(receivedPingCheck, DeliveryMethod.Unreliable);
                    //     break;
                    // }
                }
            });
        }

        protected virtual void Received(Container session) { }

        private static bool GetLocalPlayerId(Container session, out int localPlayerId)
        {
            if (session.With(out LocalPlayerProperty localPlayerProperty) && localPlayerProperty.WithValue)
            {
                localPlayerId = localPlayerProperty;
                return true;
            }
            localPlayerId = default;
            return false;
        }

        public override Ray GetRayForPlayerId(int playerId) => GetRayForPlayer(m_PlayerPredictionHistory.Peek());

        protected override void RollbackHitboxes(int playerId)
        {
            if (!DebugBehavior.Singleton.isDebugMode) return;
            for (var i = 0; i < m_Modifier.Length; i++)
            {
                // int copiedPlayerId = i;
                // Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).GetPlayer(copiedPlayerId);
                //
                // Container render = m_RenderSession.GetPlayer(i).Clone();
                //
                // float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(GetSettings().TickInterval) * 3;
                // RenderInterpolatedPlayer<LocalizedClientStampComponent>(Time.realtimeSinceStartup - rollback, render, m_SessionHistory.Size, GetInHistory);
                //
                // PlayerModifierDispatcherBehavior modifier = m_Modifier[i];
                // modifier.EvaluateHitboxes(i, render);

                Container recentPlayer = m_Visuals[i].GetRecentPlayer();
                if (i == 0 && recentPlayer != null)
                    SendDebug(recentPlayer);
            }
        }

        private void SendDebug(Container player)
        {
            var debug = new DebugClientView(player.ElementTypes);
            debug.CopyFrom(player);
            m_Socket.SendToServer(debug, DeliveryMethod.ReliableOrdered);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}