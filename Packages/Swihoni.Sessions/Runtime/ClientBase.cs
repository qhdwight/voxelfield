using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    public abstract class ClientBase : NetworkedSessionBase
    {
        private readonly Container m_RenderSession;
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;
        private float? m_ServerReceiveTime;

        public IPEndPoint IpEndPoint { get; }

        protected ClientBase(ISessionGameObjectLinker linker, IPEndPoint ipEndPoint, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                             IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            IpEndPoint = ipEndPoint;
            m_RenderSession = new Container(sessionElements);
            if (m_RenderSession.Has(out PlayerContainerArrayProperty players))
                players.SetAll(() => new Container(playerElements));
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(250, () => m_EmptyClientCommands.Clone());
            // TODO:refactor zeroing
            m_CommandHistory.Peek().Require<CameraComponent>().Zero();
            m_PlayerPredictionHistory = new CyclicArray<Container>(250, () =>
            {
                IEnumerable<Type> predictedElements = playerElements.Append(typeof(ClientStampComponent));
                return new Container(predictedElements);
            });
            // TODO:refactor zeroing
            m_PlayerPredictionHistory.Peek().Zero();
            m_PlayerPredictionHistory.Peek().Require<ClientStampComponent>().Reset();
            ForEachPlayer(player => player.Add(typeof(LocalizedClientStampComponent)));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(IpEndPoint);
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_EmptyClientCommands);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_EmptyServerSession);
        }

        private void UpdateInputs(int localPlayerId) => m_Modifier[localPlayerId].ModifyCommands(this, m_CommandHistory.Peek());

        protected override void Input(float time, float delta)
        {
            if (!GetLocalPlayerId(m_SessionHistory.Peek(), out int localPlayerId))
                return;

            UpdateInputs(localPlayerId);
            m_Modifier[localPlayerId].ModifyTrusted(this, localPlayerId, m_CommandHistory.Peek(), m_CommandHistory.Peek(), delta);
        }

        protected override void Render(float renderTime)
        {
            base.Render(renderTime);
            if (!m_RenderSession.Has(out PlayerContainerArrayProperty renderPlayers) || !GetLocalPlayerId(m_SessionHistory.Peek(), out int localPlayerId))
                return;

            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isLocalPlayer = playerId == localPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                SessionSettingsComponent settings = GetSettings();
                if (isLocalPlayer)
                {
                    Container GetInHistory(int historyIndex) => m_PlayerPredictionHistory.Get(-historyIndex);
                    float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(settings.TickInterval);
                    RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayer, m_PlayerPredictionHistory.Size, GetInHistory);
                    renderPlayer.MergeSet(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(settings.TickInterval) * 3;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(renderTime - rollback, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayer, isLocalPlayer);
                m_PlayerHud.Render(renderPlayers[localPlayerId]);
            }
        }

        protected override void Tick(uint tick, float time, float duration)
        {
            base.Tick(tick, time, duration);
            if (GetLocalPlayerId(m_SessionHistory.Peek(), out int localPlayerId))
            {
                UpdateInputs(localPlayerId);
                Predict(tick, time, localPlayerId);
            }
            Send();
            Receive(time);

            HandleTimeouts(time);
        }

        private void HandleTimeouts(float time)
        {
            if (!m_ServerReceiveTime.HasValue || Mathf.Abs(m_ServerReceiveTime.Value - time) < 2.0f) return;

            Debug.LogWarning($"[{GetType().Name}] Disconnected due to stale connection!");
            Dispose();
        }

        private void Predict(uint tick, float time, int localPlayerId)
        {
            // TODO: refactor into common logic of claiming and resetting
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.Has(out ClientStampComponent predictedStamp))
            {
                predictedPlayer.CopyFrom(previousPredictedPlayer);
                commands.CopyFrom(previousCommand);

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
                predictedCommands.Require<ClientStampComponent>().CopyFrom(predictedStamp);
                predictedPlayer.MergeSet(predictedCommands);
                if (predictedStamp.duration.HasValue)
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, predictedPlayer, predictedCommands, predictedStamp.duration);

                DebugBehavior.Singleton.Predicted = predictedPlayer;
            }
        }

        private void Send() { m_Socket.SendToServer(m_CommandHistory.Peek()); }

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
                {
                    Container predictedPlayer = m_PlayerPredictionHistory.Get(-playerHistoryIndex);
                    if (predictedPlayer.Require<ClientStampComponent>().tick != targetTick) continue;
                    var areEqual = true;
                    ElementExtensions.NavigateZipped((e1, e2) =>
                    {
                        switch (e1)
                        {
                            // TODO: refactor use attribute instead
                            case CameraComponent _:
                                return Navigation.Skip;
                            case PropertyBase p1 when e2 is PropertyBase p2 && !p1.Equals(p2):
                                areEqual = false;
                                Debug.LogWarning($"Prediction error with {p1.GetType().Name} with predicted: {p1} and verified: {p2}");
                                return Navigation.Exit;
                        }
                        return Navigation.Continue;
                    }, predictedPlayer, serverPlayer);
                    if (areEqual) continue;
                }
                /* Was not predicted properly */
                // Place base from verified server
                m_PlayerPredictionHistory.Get(-playerHistoryIndex).CopyFrom(serverPlayer);
                // Replay old commands up until most recent to get back on track
                for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
                {
                    ClientCommandsContainer commands = m_CommandHistory.Get(-commandHistoryIndex);
                    Container predictedPlayer = m_PlayerPredictionHistory.Get(-commandHistoryIndex);
                    ClientStampComponent stamp = predictedPlayer.Require<ClientStampComponent>().Clone(); // TODO: performance remove clone
                    predictedPlayer.CopyFrom(m_PlayerPredictionHistory.Get(-commandHistoryIndex - 1));
                    predictedPlayer.Require<ClientStampComponent>().CopyFrom(stamp);
                    m_Modifier[localPlayerId].ModifyChecked(this, localPlayerId, predictedPlayer, commands, commands.Require<ClientStampComponent>().duration);
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
                        ServerSessionContainer previousServerSession = m_SessionHistory.Peek(),
                                               serverSession = m_SessionHistory.ClaimNext();
                        serverSession.CopyFrom(previousServerSession);
                        serverSession.MergeSet(receivedServerSession);

                        UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                        if (previousServerTick.HasValue && serverSession.Require<ServerStampComponent>().tick <= previousServerTick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            break;
                        }
                        
                        var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
                        for (var playerId = 0; playerId < serverPlayers.Length; playerId++)
                        {
                            Container serverPlayer = serverPlayers[playerId];
                            var healthProperty = serverPlayer.Require<HealthProperty>();
                            if (!healthProperty.HasValue || healthProperty.IsDead) continue;
                            
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
                                Debug.LogWarning($"[{GetType().Name}] Client reset");
                                localizedServerTime.Value = time;
                            }
                        }

                        // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                        CheckPrediction(serverSession);

                        break;
                    }
                }
            });
        }

        protected static bool GetLocalPlayerId(Container session, out int localPlayerId)
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

        public override void AboutToRaycast(int playerId) { }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}