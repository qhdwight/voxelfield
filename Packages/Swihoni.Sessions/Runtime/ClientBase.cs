using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public abstract class ClientBase : NetworkedSessionBase
    {
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;
        private float? m_ServerReceiveTime;
        public int Resets { get; private set; }

        public IPEndPoint IpEndPoint { get; }

        protected ClientBase(ISessionGameObjectLinker linker, IPEndPoint ipEndPoint, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                             IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            IpEndPoint = ipEndPoint;
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(250, () => m_EmptyClientCommands.Clone());
            // TODO:refactor zeroing
            ZeroCommand(m_CommandHistory.Peek());
            m_PlayerPredictionHistory = new CyclicArray<Container>(250, () => new Container(playerElements.Append(typeof(ClientStampComponent))));
            m_PlayerPredictionHistory.Peek().Zero();
            m_PlayerPredictionHistory.Peek().Require<ClientStampComponent>().Reset();

            foreach (ServerSessionContainer session in m_SessionHistory)
            {
                session.Add(typeof(LocalizedClientStampComponent));
                foreach (Container player in session.Require<PlayerContainerArrayProperty>())
                    player.Add(typeof(LocalizedClientStampComponent));
            }
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(IpEndPoint);
            RegisterMessages(m_Socket);
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
            if (m_RenderSession.Without(out PlayerContainerArrayProperty renderPlayers) || !GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
                return;

            base.Render(renderTime);

            SessionSettingsComponent settings = GetSettings();
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isLocalPlayer = playerId == localPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                if (isLocalPlayer)
                {
                    Container GetInHistory(int historyIndex) => m_PlayerPredictionHistory.Get(-historyIndex);
                    float rollback = settings.TickInterval * 2;
                    RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayer, m_PlayerPredictionHistory.Size, GetInHistory);
                    renderPlayer.FastMergeSet(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    float rollback = settings.TickInterval * 3;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(renderTime - rollback, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayer, isLocalPlayer);
                m_PlayerHud.Render(renderPlayers[localPlayerId]);
            }
            // RenderEntities<LocalizedClientStampComponent>(renderTime, settings.TickInterval * 2);
        }

        protected override void Tick(uint tick, float time, float duration)
        {
            GetSettings().tickRate.IfPresent(tickRate => Time.fixedDeltaTime = 1.0f / tickRate);

            base.Tick(tick, time, duration);

            Profiler.BeginSample("Client Predict");
            if (GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
            {
                UpdateInputs(localPlayerId);
                Predict(tick, time, localPlayerId);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Client Send");
            Send();
            Profiler.EndSample();

            Profiler.BeginSample("Client Receive");
            Receive(time);
            HandleTimeouts(time);
            Profiler.EndSample();
        }

        private void HandleTimeouts(float time)
        {
            if (!m_ServerReceiveTime.HasValue || Mathf.Abs(m_ServerReceiveTime.Value - time) < 2.0f) return;

            Debug.LogWarning($"[{GetType().Name}] Disconnected due to stale connection!");
            Dispose();
        }

        private void Predict(uint tick, float time, int localPlayerId)
        {
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.Has(out ClientStampComponent predictedStamp))
            {
                predictedPlayer.FastCopyFrom(previousPredictedPlayer);
                commands.FastCopyFrom(previousCommand);

                predictedStamp.tick.Value = tick;
                predictedStamp.time.Value = time;
                var previousClientStamp = previousPredictedPlayer.Require<ClientStampComponent>();
                if (previousClientStamp.time.HasValue)
                {
                    float lastTime = previousClientStamp.time.OrElse(time),
                          duration = time - lastTime;
                    predictedStamp.duration.Value = duration;
                }

                // Inject trusted component
                ClientCommandsContainer predictedCommands = m_CommandHistory.Peek();
                predictedCommands.Require<ClientStampComponent>().FastCopyFrom(predictedStamp);
                predictedPlayer.FastMergeSet(predictedCommands);
                if (predictedStamp.duration.HasValue)
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, predictedPlayer, predictedCommands, predictedStamp.duration);
            }
        }

        private void Send() => m_Socket.SendToServer(m_CommandHistory.Peek());

        private void CheckPrediction(Container serverSession)
        {
            if (!GetLocalPlayerId(serverSession, out int localPlayerId))
                return;

            Container serverPlayer = serverSession.GetPlayer(localPlayerId);
            UIntProperty targetTick = serverPlayer.Require<ClientStampComponent>().tick;

            if (!targetTick.HasValue)
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
                    if (type.IsDefined(typeof(OnlyServerTrusted)))
                    {
                        latestPredictedElement.FastMergeSet(serverElement);
                        return Navigation.SkipDescendends;
                    }
                    if (type.IsDefined(typeof(ClientTrusted)))
                        return Navigation.SkipDescendends;
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
                // Place base from verified server
                predictedPlayer.FastCopyFrom(serverPlayer);
                // Replay old commands up until most recent to get back on track
                for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
                {
                    ClientCommandsContainer commands = m_CommandHistory.Get(-commandHistoryIndex);
                    Container pastPredictedPlayer = m_PlayerPredictionHistory.Get(-commandHistoryIndex);
                    ClientStampComponent stamp = pastPredictedPlayer.Require<ClientStampComponent>().Clone(); // TODO:performance remove clone
                    pastPredictedPlayer.FastCopyFrom(m_PlayerPredictionHistory.Get(-commandHistoryIndex - 1));
                    pastPredictedPlayer.Require<ClientStampComponent>().FastCopyFrom(stamp);
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, pastPredictedPlayer, commands, commands.Require<ClientStampComponent>().duration);
                }
                break;
            }
        }

        private void Receive(float time)
        {
            m_Socket.PollReceived((ipEndPoint, message) =>
            {
                m_ServerReceiveTime = time;
                switch (message)
                {
                    case ServerSessionContainer receivedServerSession:
                    {
                        Profiler.BeginSample("Client Receive Setup");
                        ServerSessionContainer previousServerSession = m_SessionHistory.Peek(),
                                               serverSession = m_SessionHistory.ClaimNext();
                        serverSession.FastCopyFrom(previousServerSession);
                        serverSession.FastMergeSet(receivedServerSession);
                        Profiler.EndSample();

                        UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                        if (previousServerTick.HasValue && serverSession.Require<ServerStampComponent>().tick <= previousServerTick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            break;
                        }

                        {
                            FloatProperty serverTime = serverSession.Require<ServerStampComponent>().time,
                                          localizedServerTime = serverSession.Require<LocalizedClientStampComponent>().time;

                            if (localizedServerTime.HasValue)
                                localizedServerTime.Value += serverTime - previousServerSession.Require<ServerStampComponent>().time;
                            else
                                localizedServerTime.Value = time;
                            
                            if (Mathf.Abs(localizedServerTime.Value - time) > GetSettings(serverSession).TickInterval * 3)
                            {
                                Resets++;
                                localizedServerTime.Value = time;
                            }
                        }

                        Profiler.BeginSample("Client Update Players");
                        var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
                        for (var playerId = 0; playerId < serverPlayers.Length; playerId++)
                        {
                            Container serverPlayer = serverPlayers[playerId];
                            var healthProperty = serverPlayer.Require<HealthProperty>();
                            if (healthProperty.WithoutValue || healthProperty.IsDead) continue;
                            /* We have been acknowledged by the server */

                            FloatProperty serverTime = serverPlayer.Require<ServerStampComponent>().time,
                                          localizedServerTime = serverPlayer.Require<LocalizedClientStampComponent>().time;

                            if (localizedServerTime.HasValue)
                                localizedServerTime.Value += serverTime - previousServerSession.GetPlayer(playerId).Require<ServerStampComponent>().time;
                            else
                                localizedServerTime.Value = time;

                            GetLocalPlayerId(serverSession, out int localPlayerId);
                            if (playerId != localPlayerId) m_Modifier[playerId].Synchronize(serverPlayer);

                            if (Mathf.Abs(localizedServerTime.Value - time) > GetSettings(serverSession).TickInterval * 3)
                            {
                                // Debug.LogWarning($"[{GetType().Name}] Client reset");
                                Resets++;
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
                    case PingCheckComponent receivedPingCheck:
                    {
                        m_Socket.SendToServer(receivedPingCheck);
                        break;
                    }
                }
            });
        }

        private static bool GetLocalPlayerId(Container session, out int localPlayerId)
        {
            if (session.Has(out LocalPlayerProperty localPlayerProperty) && localPlayerProperty.HasValue)
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

        public void SendDebug(Container player)
        {
            var debug = new DebugClientView(player.ElementTypes);
            debug.FastCopyFrom(player);
            m_Socket.SendToServer(debug);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}