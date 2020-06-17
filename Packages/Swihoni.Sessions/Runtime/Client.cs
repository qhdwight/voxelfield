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
    public sealed class Client : NetworkedSessionBase
    {
        private readonly string m_ConnectKey;
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;

        public int PredictionErrors { get; private set; }
        public override ComponentSocketBase Socket => m_Socket;

        public Client(SessionElements elements, IPEndPoint ipEndPoint, string connectKey, SessionInjectorBase injector)
            : base(elements, ipEndPoint, injector)
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

        protected override void Input(uint timeUs, uint deltaUs)
        {
            if (!GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
                return;

            UpdateInputs(localPlayerId);
            m_Modifier[localPlayerId].ModifyTrusted(this, localPlayerId, m_CommandHistory.Peek(), m_CommandHistory.Peek(), deltaUs);
        }

        protected override void Render(uint renderTimeUs)
        {
            if (m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers) || !GetLocalPlayerId(GetLatestSession(), out int localPlayerId))
                return;

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (tickRate.WithoutValue) return;

            base.Render(renderTimeUs);

            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isLocalPlayer = playerId == localPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                if (isLocalPlayer)
                {
                    Container GetInHistory(int historyIndex) => m_PlayerPredictionHistory.Get(-historyIndex);
                    uint playerRenderTimeUs = renderTimeUs - tickRate.TickIntervalUs;
                    RenderInterpolatedPlayer<ClientStampComponent>(playerRenderTimeUs, renderPlayer, m_PlayerPredictionHistory.Size, GetInHistory);
                    renderPlayer.MergeFrom(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[copiedPlayerId];

                    uint playerRenderTimeUs = renderTimeUs - tickRate.PlayerRenderIntervalUs;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(playerRenderTimeUs, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayer, isLocalPlayer);
                if (isLocalPlayer) m_PlayerHud.Render(renderPlayers[localPlayerId]);
            }
            RenderEntities<LocalizedClientStampComponent>(renderTimeUs, tickRate.TickIntervalUs * 2u);
        }

        protected override void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Profiler.BeginSample("Client Predict");
            Container latestSession = GetLatestSession();
            if (GetLocalPlayerId(latestSession, out int localPlayerId))
            {
                m_Injector.OnSettingsTick(latestSession);
                UpdateInputs(localPlayerId);
                Predict(tick, timeUs, localPlayerId);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Client Send");
            SendCommand();
            Profiler.EndSample();

            Profiler.BeginSample("Client Receive");
            Receive(timeUs);
            Profiler.EndSample();

            base.Tick(tick, timeUs, durationUs);
        }

        private void Predict(uint tick, uint timeUs, int localPlayerId)
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
                predictedStamp.timeUs.Value = timeUs;
                var previousClientStamp = previousPredictedPlayer.Require<ClientStampComponent>();
                if (previousClientStamp.timeUs.WithValue)
                {
                    uint lastTime = previousClientStamp.timeUs.Else(timeUs),
                         durationUs = timeUs - lastTime;
                    predictedStamp.durationUs.Value = durationUs;
                }

                // Inject trusted component
                commands.Require<ClientStampComponent>().CopyFrom(predictedStamp);
                predictedPlayer.MergeFrom(commands);
                if (predictedStamp.durationUs.WithValue)
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, predictedPlayer, commands, predictedStamp.durationUs);
            }
        }

        private void SendCommand() => m_Socket.SendToServer(m_CommandHistory.Peek(), DeliveryMethod.ReliableUnordered);

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
                ElementExtensions.NavigateZipped((_predicted, _latestPredicted, _server) =>
                {
                    Type type = _predicted.GetType();
                    if (type.IsDefined(typeof(OnlyServerTrustedAttribute)))
                    {
                        _latestPredicted.MergeFrom(_server);
                        return Navigation.SkipDescendents;
                    }
                    if (type.IsDefined(typeof(ClientTrustedAttribute)))
                        return Navigation.SkipDescendents;
                    switch (_predicted)
                    {
                        case FloatProperty f1 when _server is FloatProperty f2 && f1.TryAttribute(out PredictionToleranceAttribute fPredictionToleranceAttribute)
                                                                               && !f1.CheckWithinTolerance(f2, fPredictionToleranceAttribute.tolerance):
                        case VectorProperty v1 when _server is VectorProperty v2 && v1.TryAttribute(out PredictionToleranceAttribute vPredictionToleranceAttribute)
                                                                                 && !v1.CheckWithinTolerance(v2, vPredictionToleranceAttribute.tolerance):
                        case PropertyBase p1 when _server is PropertyBase p2 && !p1.Equals(p2):
                            areEqual = false;
                            Debug.LogWarning($"Prediction error with {_predicted.GetType().Name} with predicted: {_predicted} and verified: {_server}");
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
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, pastPredictedPlayer, commands, commands.Require<ClientStampComponent>().durationUs);
                }
                break;
            }
        }

        private void Receive(uint timeUs)
        {
            m_Socket.PollReceived((peer, message) =>
            {
                switch (message)
                {
                    case ServerSessionContainer receivedServerSession:
                    {
                        Profiler.BeginSample("Client Receive Setup");
                        ServerSessionContainer previousServerSession = m_SessionHistory.Peek(),
                                               serverSession = m_SessionHistory.ClaimNext();
                        serverSession.CopyFrom(previousServerSession);
                        UpdateCurrentSessionFromReceived(serverSession, receivedServerSession);
                        // TODO:refactor truncation
                        serverSession.Require<LocalizedClientStampComponent>().CopyFrom(previousServerSession.Require<LocalizedClientStampComponent>());
                        Profiler.EndSample();

                        m_Injector.OnReceive(serverSession);

                        uint serverTick = serverSession.Require<ServerStampComponent>().tick;
                        UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                        if (previousServerTick.WithValue && serverTick <= previousServerTick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            break;
                        }
                        if (previousServerTick.WithValue)
                        {
                            checked
                            {
                                uint delta = serverTick - previousServerTick;
                                m_CommandHistory.Peek().Require<AcknowledgedServerTickProperty>().Value = serverTick - delta + 1;
                            }
                        }
                        {
                            // TODO:refactor make function
                            UIntProperty serverTimeUs = serverSession.Require<ServerStampComponent>().timeUs,
                                         localizedServerTimeUs = serverSession.Require<LocalizedClientStampComponent>().timeUs;

                            if (localizedServerTimeUs.WithValue)
                                localizedServerTimeUs.Value += checked(serverTimeUs - previousServerSession.Require<ServerStampComponent>().timeUs);
                            else localizedServerTimeUs.Value = timeUs;

                            long delta = localizedServerTimeUs.Value - (long) timeUs;
                            if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                            {
                                ResetErrors++;
                                localizedServerTimeUs.Value = timeUs;
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

                            UIntProperty serverTimeUs = serverPlayer.Require<ServerStampComponent>().timeUs,
                                         localizedServerTimeUs = serverPlayer.Require<LocalizedClientStampComponent>().timeUs;

                            if (localizedServerTimeUs.WithValue)
                                localizedServerTimeUs.Value += checked(serverTimeUs - previousServerSession.GetPlayer(playerId).Require<ServerStampComponent>().timeUs);
                            else localizedServerTimeUs.Value = timeUs;

                            GetLocalPlayerId(serverSession, out int localPlayerId);
                            if (playerId != localPlayerId) m_Modifier[playerId].Synchronize(serverPlayer);

                            long delta = localizedServerTimeUs.Value - (long) timeUs;
                            if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                            {
                                ResetErrors++;
                                localizedServerTimeUs.Value = timeUs;
                            }
                        }
                        Profiler.EndSample();

                        // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                        Profiler.BeginSample("Client Check Prediction");
                        CheckPrediction(serverSession);
                        Profiler.EndSample();

                        break;
                    }
                }
            });
        }

        private static void UpdateCurrentSessionFromReceived(ElementBase serverSession, ElementBase receivedServerSession)
        {
            ElementExtensions.NavigateZipped((_current, _received) =>
            {
                if (_current is PropertyBase _currentProperty && _received is PropertyBase _receivedProperty)
                {
                    if (_current.GetType().IsDefined(typeof(AdditiveAttribute)) || !_receivedProperty.WasSame)
                        _currentProperty.SetTo(_receivedProperty);
                }
                return Navigation.Continue;
            }, serverSession, receivedServerSession);
        }

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

                if (i == 0 && recentPlayer != null) SendDebug(recentPlayer);
            }
        }

        private void SendDebug(Container player)
        {
            DebugClientView debug = m_EmptyDebugClientView.Clone();
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