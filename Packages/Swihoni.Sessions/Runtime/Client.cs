using System;
using System.Linq;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public sealed class Client : NetworkedSessionBase, IReceiver
    {
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;

        public int PredictionErrors { get; private set; }
        public override ComponentSocketBase Socket => m_Socket;

        public Client(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector)
            : base(elements, ipEndPoint, injector)
        {
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(HistoryCount, () => m_EmptyClientCommands.Clone());
            SetFirstCommand(m_CommandHistory.Peek());
            m_PlayerPredictionHistory = new CyclicArray<Container>(HistoryCount, () => new Container(elements.playerElements.Append(typeof(ClientStampComponent))));
            Container firstPrediction = m_PlayerPredictionHistory.Peek();
            firstPrediction.Zero();
            firstPrediction.Require<ClientStampComponent>().Clear();

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
            m_Socket = new ComponentClientSocket(IpEndPoint, m_Injector.GetConnectWriter());
            m_Socket.Listener.PeerDisconnectedEvent += OnDisconnect;
            m_Socket.Receiver = this;
            RegisterMessages(m_Socket);
        }

        private void OnDisconnect(NetPeer peer, DisconnectInfo disconnect)
        {
            if (disconnect.AdditionalData.TryGetString(out string reason))
                Debug.LogWarning($"Disconnected for reason: {reason}");
            Dispose();
        }

        private void UpdateInputs(Container player, int localPlayerId)
        {
            ClientCommandsContainer commands = m_CommandHistory.Peek();
            GetPlayerModifier(player, localPlayerId).ModifyCommands(this, commands, localPlayerId);
            _indexer = localPlayerId; // Prevent closure allocation
            _session = this;
            _container = commands;
            ForEachSessionInterface(@interface => @interface.ModifyLocalTrusted(_indexer, _session, _container));
        }

        public override Container GetLocalCommands() => m_CommandHistory.Peek();

        protected override void Input(uint timeUs, uint durationUs)
        {
            Container verifiedLatestSession = GetLatestSession();
            if (!GetLocalPlayerId(verifiedLatestSession, out int localPlayerId))
                return;
            Container verifiedPlayer = GetModifyingPayerFromId(localPlayerId, verifiedLatestSession);
            UpdateInputs(verifiedPlayer, localPlayerId);
            var context = new SessionContext(this, commands: m_CommandHistory.Peek(), playerId: localPlayerId, player: m_CommandHistory.Peek(),
                                             timeUs: timeUs, durationUs: durationUs);
            GetPlayerModifier(verifiedPlayer, localPlayerId).ModifyTrusted(context, verifiedPlayer);
        }

        // private static CyclicArray<Container> _predictionHistory;

        private static CyclicArray<Container> _predictionHistory;

        protected override void Render(uint renderTimeUs)
        {
            Profiler.BeginSample("Client Render Setup");
            if (IsLoading || m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers)
                          || m_RenderSession.Without(out LocalPlayerId renderLocalPlayerId)
                          || !GetLocalPlayerId(GetLatestSession(), out int actualLocalPlayerId))
            {
                Profiler.EndSample();
                return;
            }

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (tickRate.WithoutValue) return;

            m_RenderSession.SetTo(GetLatestSession());
            Profiler.EndSample();

            Profiler.BeginSample("Client Spectate Setup");
            bool isSpectating = IsSpectating(m_RenderSession, renderPlayers, actualLocalPlayerId, out SpectatingPlayerId spectatingPlayerId);
            renderLocalPlayerId.Value = isSpectating ? spectatingPlayerId.Value : (byte) actualLocalPlayerId;
            Profiler.EndSample();

            Profiler.BeginSample("Client Render Players");
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isActualLocalPlayer = playerId == actualLocalPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                if (isActualLocalPlayer)
                {
                    uint playerRenderTimeUs = renderTimeUs - tickRate.TickIntervalUs;
                    _predictionHistory = m_PlayerPredictionHistory;
                    RenderInterpolatedPlayer<ClientStampComponent>(playerRenderTimeUs, renderPlayer, m_PlayerPredictionHistory.Size,
                                                                   historyIndex => _predictionHistory.Get(-historyIndex));
                    renderPlayer.MergeFrom(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    uint playerRenderTimeUs = renderTimeUs - tickRate.PlayerRenderIntervalUs;
                    _serverHistory = m_SessionHistory;
                    _indexer = playerId;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(playerRenderTimeUs, renderPlayer, m_SessionHistory.Size,
                                                                            historyIndex =>
                                                                                _serverHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[_indexer]);
                }
                PlayerVisualsDispatcherBehavior visuals = GetPlayerVisuals(renderPlayer, playerId);
                bool isPossessed = isActualLocalPlayer && !isSpectating || isSpectating && playerId == spectatingPlayerId;
                var context = new SessionContext(this, m_RenderSession, playerId: playerId, player: renderPlayer);
                if (visuals) visuals.Render(context, isPossessed);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Client Render Interfaces");
            RenderInterfaces(m_RenderSession);
            Profiler.EndSample();

            Profiler.BeginSample("Client Render Entities");
            RenderEntities<LocalizedClientStampComponent>(renderTimeUs, tickRate.TickIntervalUs * 2u);
            Profiler.EndSample();

            Profiler.BeginSample("Client Render Mode");
            ModeManager.GetMode(m_RenderSession).Render(this, m_RenderSession);
            Profiler.EndSample();
        }

        internal static bool IsSpectating(Container renderSession, PlayerContainerArrayElement renderPlayers, int actualLocalPlayerId, out SpectatingPlayerId spectatingPlayerId)
        {
            bool isSpectating = ModeManager.GetMode(renderSession).CanSpectate(renderSession, renderPlayers[actualLocalPlayerId]);
            spectatingPlayerId = renderSession.Require<SpectatingPlayerId>();
            if (isSpectating)
            {
                Container localPlayer = renderPlayers[actualLocalPlayerId];
                if (spectatingPlayerId.WithValue)
                {
                    Container currentPlayer = renderPlayers[spectatingPlayerId];
                    if (currentPlayer.Require<HealthProperty>().IsInactiveOrDead || currentPlayer.Require<TeamProperty>() != localPlayer.Require<TeamProperty>())
                        spectatingPlayerId.Clear();
                }
                if (spectatingPlayerId.WithoutValue)
                {
                    for (byte playerId = 0; playerId < renderPlayers.Length; playerId++)
                    {
                        if (playerId == actualLocalPlayerId) continue;
                        Container renderPlayer = renderPlayers[playerId];
                        if (renderPlayer.Require<HealthProperty>().IsInactiveOrDead) continue;
                        if (renderPlayer.Require<TeamProperty>() == localPlayer.Require<TeamProperty>())
                        {
                            spectatingPlayerId.Value = playerId;
                            break;
                        }
                    }
                }
                if (spectatingPlayerId.WithoutValue) isSpectating = false;
            }
            if (!isSpectating)
            {
                spectatingPlayerId.Clear();
            }
            return isSpectating;
        }

        protected override void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Profiler.BeginSample("Client Predict");
            Container latestSession = GetLatestSession();
            if (GetLocalPlayerId(latestSession, out int localPlayerId))
            {
                m_Injector.OnSettingsTick(latestSession);
                Predict(tick, timeUs, localPlayerId); // Advances commands
            }
            Profiler.EndSample();

            Profiler.BeginSample("Client Send");
            SendCommand();
            Profiler.EndSample();

            Profiler.BeginSample("Client Receive");
            Receive(timeUs);
            Profiler.EndSample();

            ClearSingleTicks(m_CommandHistory.Peek());

            base.Tick(tick, timeUs, durationUs);
        }

        private void Receive(uint timeUs)
        {
            _timeUs = timeUs;
            m_Socket.PollEvents();
        }

        private static uint _timeUs;

        void IReceiver.OnReceive(NetPeer fromPeer, NetDataReader reader, byte code)
        {
            switch (code)
            {
                case ServerSessionCode:
                {
                    Profiler.BeginSample("Client Receive Setup");
                    ServerSessionContainer previousServerSession = m_SessionHistory.Peek();

                    m_EmptyServerSession.Deserialize(reader);
                    ServerSessionContainer receivedServerSession = m_EmptyServerSession;

                    uint serverTick = receivedServerSession.Require<ServerStampComponent>().tick;
                    UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                    ServerSessionContainer serverSession;
                    var isMostRecent = true;
                    if (previousServerTick.WithValue)
                    {
                        var delta = checked((int) (serverTick - (long) previousServerTick));
                        if (delta > 0)
                        {
                            m_CommandHistory.Peek().Require<AcknowledgedServerTickProperty>().Value = serverTick;
                            for (var i = 0; i < delta - 1; i++) // We skipped tick(s). Reserve spaces to fill later
                            {
                                ServerSessionContainer reserved = m_SessionHistory.ClaimNext();
                                reserved.SetTo(previousServerSession);
                            }
                            serverSession = m_SessionHistory.ClaimNext();
                        }
                        else
                        {
                            // We received an old tick. Fill in history
                            serverSession = m_SessionHistory.Get(delta);
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            isMostRecent = false;
                        }
                    }
                    else serverSession = m_SessionHistory.ClaimNext();

                    UpdateCurrentSessionFromReceived(previousServerSession, serverSession, receivedServerSession);
                    Profiler.EndSample();

                    m_Injector.OnClientReceive(serverSession);
                    RenderVerified(serverSession);

                    if (!isMostRecent)
                    {
                        Debug.LogWarning("Is not most recent!");
                        break;
                    }

                    /* Most Recent */

                    {
                        // TODO:refactor make class

                        var serverStamp = serverSession.Require<ServerStampComponent>();
                        var previousServerStamp = serverSession.Require<LocalizedClientStampComponent>();
                        UIntProperty serverTimeUs = serverStamp.timeUs, localizedServerTimeUs = previousServerStamp.timeUs;

                        if (localizedServerTimeUs.WithValue)
                        {
                            uint previousTimeUs = previousServerSession.Require<ServerStampComponent>().timeUs;
                            if (serverTimeUs >= previousTimeUs) localizedServerTimeUs.Value += checked(serverTimeUs - previousTimeUs);
                            else Debug.LogError($"Updated server session time was less. Current: {serverStamp}; Previous: {previousServerStamp} μs");
                        }
                        else localizedServerTimeUs.Value = _timeUs;

                        long delta = localizedServerTimeUs.Value - (long) _timeUs;
                        if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                        {
                            ResetErrors++;
                            localizedServerTimeUs.Value = _timeUs;
                        }
                        AnalysisLogger.AddDataPoint(string.Empty, localizedServerTimeUs.Value, _timeUs, serverTimeUs.Value);
                    }

                    Profiler.BeginSample("Client Update Players");
                    var serverPlayers = serverSession.Require<PlayerContainerArrayElement>();
                    bool isLocalPlayerOnServer = GetLocalPlayerId(serverSession, out int localPlayerId);
                    for (var playerId = 0; playerId < serverPlayers.Length; playerId++)
                    {
                        Container serverPlayer = serverPlayers[playerId];
                        var healthProperty = serverPlayer.Require<HealthProperty>();
                        UIntProperty localizedServerTimeUs = serverPlayer.Require<LocalizedClientStampComponent>().timeUs;
                        if (healthProperty.WithoutValue)
                            localizedServerTimeUs.Clear(); // Is something a client only has so we have to clear it
                        if (healthProperty.IsInactiveOrDead)
                            continue;
                        /* Valid player */

                        UIntProperty serverTimeUs = serverPlayer.Require<ServerStampComponent>().timeUs;

                        if (localizedServerTimeUs.WithValue)
                        {
                            uint previousTimeUs = previousServerSession.GetPlayer(playerId).Require<ServerStampComponent>().timeUs;
                            if (serverTimeUs >= previousTimeUs) localizedServerTimeUs.Value += checked(serverTimeUs - previousTimeUs);
#if UNITY_EDITOR
                            else Debug.LogWarning($"Updated server player time was greater. Current: {serverTimeUs.Value} μs; Previous: {previousTimeUs} μs");
#endif
                        }
                        else localizedServerTimeUs.Value = _timeUs;

                        if (playerId != localPlayerId) GetPlayerModifier(serverPlayer, playerId).Synchronize(new SessionContext(player: serverPlayer));

                        long delta = localizedServerTimeUs.Value - (long) _timeUs;
                        if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                        {
                            ResetErrors++;
                            localizedServerTimeUs.Value = _timeUs;
                        }
                    }
                    Profiler.EndSample();

                    // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                    Profiler.BeginSample("Client Check Prediction");
                    if (isLocalPlayerOnServer)
                    {
                        Container serverPlayer = serverSession.GetPlayer(localPlayerId);
                        CheckPrediction(serverPlayer, localPlayerId);
                    }
                    Profiler.EndSample();

                    ElementExtensions.NavigateZipped((_server, _command) =>
                    {
                        if (_server is PropertyBase serverProperty && serverProperty.IsOverride && _command is PropertyBase commandProperty)
                        {
                            commandProperty.SetTo(serverProperty);
                            Debug.Log($"Overriding with server: {serverProperty}");
                        }
                        return Navigation.Continue;
                    }, serverSession.GetPlayer(localPlayerId), m_CommandHistory.Peek());
                    break;
                }
            }
        }

        public static void ClearSingleTicks(ElementBase commands) =>
            commands.Navigate(_element =>
            {
                if (_element.WithAttribute<SingleTickAttribute>())
                {
                    _element.Clear();
                    return Navigation.SkipDescendents;
                }
                return Navigation.Continue;
            });

        private void Predict(uint tick, uint timeUs, int localPlayerId)
        {
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.Without(out ClientStampComponent predictedStamp)) return;

            predictedPlayer.SetTo(previousPredictedPlayer);
            commands.SetTo(previousCommand);

            if (IsLoading)
            {
                commands.Require<ClientStampComponent>().Clear();
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
                commands.Require<ClientStampComponent>().SetTo(predictedStamp);
                predictedPlayer.MergeFrom(commands);
                if (IsLoading || predictedStamp.durationUs.WithoutValue) return;

                PlayerModifierDispatcherBehavior modifier = GetPlayerModifier(predictedPlayer, localPlayerId);
                if (!modifier) return;

                var context = new SessionContext(this, GetLatestSession(), commands, localPlayerId, predictedPlayer,
                                                 timeUs: timeUs, durationUs: predictedStamp.durationUs, tickDelta: 1);
                modifier.ModifyChecked(context);
            }
        }

        // private static NetDataWriter _writer;
        //
        // private void SendCommand() => m_Socket.SendToServer(m_CommandHistory.Peek(), DeliveryMethod.ReliableUnordered, (element, writer) =>
        // {
        //     _writer = writer; // Prevent closure allocation
        //     element.Navigate(_element =>
        //     {
        //         if (_element.WithAttribute<ClientTrustedAttribute>()) _element.Serialize(_writer);
        //         return Navigation.Continue;
        //     });
        // });

        private void SendCommand() => m_Socket.SendToServer(m_CommandHistory.Peek(), DeliveryMethod.ReliableUnordered);

        private static bool _predictionIsAccurate; // Prevents heap allocation in closure

        private static readonly Func<ElementBase, ElementBase, ElementBase, Navigation> VisitPredictedFunction = VisitPredicted;

        private void CheckPrediction(Container serverPlayer, int localPlayerId)
        {
            UIntProperty targetClientTick = serverPlayer.Require<ClientStampComponent>().tick;
            if (targetClientTick.WithoutValue)
                return;

            var playerHistoryIndex = 0;
            Container basePredictedPlayer = null;
            for (; playerHistoryIndex < m_PlayerPredictionHistory.Size; playerHistoryIndex++)
            {
                Container predictedPlayer = m_PlayerPredictionHistory.Get(-playerHistoryIndex);
                if (predictedPlayer.Require<ClientStampComponent>().tick == targetClientTick)
                {
                    basePredictedPlayer = predictedPlayer;
                    break;
                }
            }
            if (basePredictedPlayer is null)
                return;

            /* We are checking predicted */
            Container latestPredictedPlayer = m_PlayerPredictionHistory.Peek();
            _predictionIsAccurate = true; // Set by the following navigation
            ElementExtensions.NavigateZipped(VisitPredictedFunction, basePredictedPlayer, latestPredictedPlayer, serverPlayer);
            if (_predictionIsAccurate)
                return;

            /* We did not predict properly */
            PredictionErrors++;
            // Place base from verified server
            basePredictedPlayer.SetTo(serverPlayer);
            // Replay old commands up until most recent to get back on track
            for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
            {
                ClientCommandsContainer commands = m_CommandHistory.Get(-commandHistoryIndex);
                Container pastPredictedPlayer = m_PlayerPredictionHistory.Get(-commandHistoryIndex);
                ClientStampComponent stamp = pastPredictedPlayer.Require<ClientStampComponent>().Clone(); // TODO:performance remove clone
                pastPredictedPlayer.SetTo(m_PlayerPredictionHistory.Get(-commandHistoryIndex - 1));
                pastPredictedPlayer.Require<ClientStampComponent>().SetTo(stamp);
                PlayerModifierDispatcherBehavior localPlayerModifier = GetPlayerModifier(pastPredictedPlayer, localPlayerId);
                if (commands.Require<ClientStampComponent>().durationUs.WithValue)
                {
                    // TODO:architecture use latest session?
                    Container serverSession = GetLatestSession();
                    var context = new SessionContext(this, serverSession, commands, localPlayerId, pastPredictedPlayer,
                                                     durationUs: commands.Require<ClientStampComponent>().durationUs, tickDelta: 1);
                    localPlayerModifier.ModifyChecked(context);
                }
                else
                {
                    Debug.LogError("Should not happen");
                }
            }
        }

        private static Navigation VisitPredicted(ElementBase _predicted, ElementBase _latestPredicted, ElementBase _server)
        {
            if (_predicted.WithAttribute<OnlyServerTrustedAttribute>())
            {
                _latestPredicted.MergeFrom(_server);
                return Navigation.SkipDescendents;
            }
            if (_predicted.WithAttribute<ClientTrustedAttribute>() || _predicted.WithAttribute<ClientNonCheckedAttribute>()) return Navigation.SkipDescendents;
            switch (_predicted)
            {
                case FloatProperty f1 when _server is FloatProperty f2 &&
                                           (f1.WithValue && f2.WithValue
                                                         && f1.TryAttribute(out PredictionToleranceAttribute fTolerance)
                                                         && !f1.CheckWithinTolerance(f2, fTolerance.tolerance)
                                         || f1.WithoutValue && f2.WithValue || f1.WithValue && f2.WithoutValue):
                case VectorProperty v1 when _server is VectorProperty v2 &&
                                            (v1.WithValue && v2.WithValue
                                                          && v1.TryAttribute(out PredictionToleranceAttribute vTolerance)
                                                          && !v1.CheckWithinTolerance(v2, vTolerance.tolerance)
                                          || v1.WithoutValue && v2.WithValue || v1.WithValue && v2.WithoutValue):
                case PropertyBase p1 when _server is PropertyBase p2 && !p1.Equals(p2):
                    _predictionIsAccurate = false;
                    if (ConfigManagerBase.Active.logPredictionErrors)
                        Debug.LogWarning($"Error with predicted: {_predicted} and verified: {_server}");
                    return Navigation.Exit;
            }
            return Navigation.Continue;
        }

        private static void UpdateCurrentSessionFromReceived(Container previousServerSession, Container serverSession, ElementBase receivedServerSession)
        {
            ElementExtensions.NavigateZipped((_previous, _current, _received) =>
            {
                if (_current is PropertyBase _currentProperty)
                {
                    var _receivedProperty = (PropertyBase) _received;
                    if (_current.WithAttribute<SingleTickAttribute>() || !_receivedProperty.WasSame)
                    {
                        _currentProperty.SetTo(_receivedProperty);
                        _currentProperty.IsOverride = _receivedProperty.IsOverride;
                    }
                    else
                    {
                        _currentProperty.SetTo((PropertyBase) _previous);
                    }
                }
                return Navigation.Continue;
            }, previousServerSession, serverSession, receivedServerSession);
            // TODO:refactor need some sort of zip longest
            serverSession.Require<LocalizedClientStampComponent>().SetTo(previousServerSession.Require<LocalizedClientStampComponent>());
            var previousArray = previousServerSession.Require<PlayerContainerArrayElement>();
            var array = serverSession.Require<PlayerContainerArrayElement>();
            for (var playerIndex = 0; playerIndex < array.Length; playerIndex++)
                array[playerIndex].Require<LocalizedClientStampComponent>().SetTo(previousArray[playerIndex].Require<LocalizedClientStampComponent>());
        }

        private static bool GetLocalPlayerId(Container session, out int localPlayerId)
        {
            if (session.With(out LocalPlayerId localPlayerProperty) && localPlayerProperty.WithValue)
            {
                localPlayerId = localPlayerProperty;
                return true;
            }
            localPlayerId = default;
            return false;
        }

        public override Ray GetRayForPlayerId(int playerId) => m_PlayerPredictionHistory.Peek().GetRayForPlayer();

        protected override void RollbackHitboxes(in SessionContext context)
        {
            if (!DebugBehavior.Singleton.SendDebug) return;
            for (var i = 0; i < MaxPlayers; i++)
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

                Container recentPlayer = ((PlayerVisualsDispatcherBehavior) PlayerManager.UnsafeVisuals[i]).GetRecentPlayer();

                if (i == 0 && recentPlayer != null) SendDebug(recentPlayer);
            }
        }

        private void SendDebug(Container player)
        {
            DebugClientView debug = m_EmptyDebugClientView.Clone();
            debug.SetTo(player);
            m_Socket.SendToServer(debug, DeliveryMethod.ReliableOrdered);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}