using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Swihoni.Components;
using Swihoni.Networking;
using Swihoni.Collections;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer()
        {
        }

        public ClientCommandsContainer(IEnumerable<Type> types) : base(types)
        {
        }
    }

    [Serializable]
    public class ClientStampComponent : StampComponent
    {
    }

    [Serializable]
    public class LocalizedClientStampComponent : StampComponent
    {
    }

    public abstract class ClientBase : NetworkedSessionBase
    {
        private readonly Container m_RenderSession;
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                             IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_RenderSession = new Container(sessionElements);
            if (m_RenderSession.If(out PlayerContainerArrayProperty players))
                players.SetAll(() => new Container(playerElements));
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(250, () => m_EmptyClientCommands.Clone());
            m_PlayerPredictionHistory = new CyclicArray<Container>(250, () =>
            {
                // IEnumerable<Type> predictedElements = playerElements.Except(new[] {typeof(HealthProperty)}).Append(typeof(StampComponent));
                IEnumerable<Type> predictedElements = playerElements.Append(typeof(ClientStampComponent));
                return new Container(predictedElements);
            });
            foreach (ServerSessionContainer serverSession in m_SessionHistory)
            foreach (Container player in serverSession.Require<PlayerContainerArrayProperty>())
                player.Add(typeof(LocalizedClientStampComponent));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Loopback, 7777));
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_EmptyClientCommands);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_EmptyServerSession);
        }

        private void UpdateInputs()
        {
            if (GetLocalPlayerId(out int localPlayerId))
                m_Modifier[localPlayerId].ModifyCommands(m_CommandHistory.Peek());
        }

        public override void Input(float time, float delta)
        {
            if (GetLocalPlayerId(out int localPlayerId))
            {
                UpdateInputs();
                m_Modifier[localPlayerId].ModifyTrusted(m_CommandHistory.Peek(), m_CommandHistory.Peek(), delta);
            }
        }

        protected override void Render(float renderTime, float tickElapsed)
        {
            if (!m_RenderSession.If(out PlayerContainerArrayProperty players) || !GetLocalPlayerId(out int localPlayerId)) return;

            for (var playerId = 0; playerId < players.Length; playerId++)
            {
                bool isLocalPlayer = playerId == localPlayerId;
                Container renderPlayer = players[playerId];
                if (isLocalPlayer)
                {
                    Container GetInHistory(int historyIndex) => m_PlayerPredictionHistory.Get(-historyIndex);
                    float rollback = DebugBehavior.Singleton.Rollback;
                    RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayer, m_PlayerPredictionHistory.Size, GetInHistory);
                    renderPlayer.MergeSet(m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    float rollback = DebugBehavior.Singleton.Rollback * 3;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(renderTime - rollback, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(renderPlayer, isLocalPlayer);
            }
        }

        protected override void Tick(uint tick, float time, float duration)
        {
            base.Tick(tick, time, duration);
            UpdateInputs();
            Predict(tick, time);
            Send();
            Receive(time);
        }

        private void Predict(uint tick, float time)
        {
            if (!GetLocalPlayerId(out int localPlayerId)) return;

            // TODO: refactor into common logic of claiming and resetting
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.If(out ClientStampComponent predictedStamp))
            {
                predictedPlayer.CopyFrom(previousPredictedPlayer);
                commands.CopyFrom(previousCommand);

                predictedStamp.tick.Value = tick;
                predictedStamp.time.Value = time;
                float lastTime = previousPredictedPlayer.Require<ClientStampComponent>().time.OrElse(time),
                      duration = time - lastTime;
                predictedStamp.duration.Value = duration;

                // Inject trusted component
                ClientCommandsContainer predictedCommands = m_CommandHistory.Peek();
                var predictedCommandsStamp = predictedCommands.Require<ClientStampComponent>();
                predictedCommandsStamp.CopyFrom(predictedStamp);
                predictedPlayer.MergeSet(predictedCommands);
                m_Modifier[localPlayerId].ModifyChecked(predictedPlayer, predictedCommands, duration);

                DebugBehavior.Singleton.Predicted = predictedPlayer;
            }
        }

        private void Send()
        {
            m_Socket.SendToServer(m_CommandHistory.Peek());
        }

        private void CheckPrediction(Container serverPlayer)
        {
            if (!GetLocalPlayerId(out int localPlayerId)) return;

            uint targetTick = serverPlayer.Require<ClientStampComponent>().tick;
            for (var playerHistoryIndex = 0; playerHistoryIndex < m_PlayerPredictionHistory.Size; playerHistoryIndex++)
            {
                {
                    Container predictedPlayer = m_PlayerPredictionHistory.Get(-playerHistoryIndex);
                    if (predictedPlayer.Require<ClientStampComponent>().tick != targetTick) continue;
                    var areEqual = true;
                    Extensions.NavigateZipped((field, e1, e2) =>
                    {
                        switch (e1)
                        {
                            // TODO: refactor use attribute instead
                            case CameraComponent _:
                                return Navigation.Skip;
                            case PropertyBase p1 when e2 is PropertyBase p2 && !p1.Equals(p2):
                                areEqual = false;
                                return Navigation.Exit;
                        }
                        return Navigation.Continue;
                    }, predictedPlayer, serverPlayer);
                    if (areEqual) continue;
                }
                /* Was not predicted properly */
                Debug.LogWarning("Prediction error");
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
                    m_Modifier[localPlayerId].ModifyChecked(predictedPlayer, commands, commands.Require<ClientStampComponent>().duration);
                }
                break;
            }
        }

        private void Receive(float time)
        {
            m_Socket.PollReceived((_, message) =>
            {
                switch (message)
                {
                    case ServerSessionContainer receivedServerSession:
                    {
                        ServerSessionContainer previousServerSession = m_SessionHistory.Peek(),
                                               serverSession = m_SessionHistory.ClaimNext();
                        serverSession.CopyFrom(previousServerSession);
                        serverSession.MergeSet(receivedServerSession);

                        if (serverSession.Require<ServerStampComponent>().tick <= previousServerSession.Require<ServerStampComponent>().tick)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Received out of order server update");
                            break;
                        }
                        
                        var serverPlayers = serverSession.Require<PlayerContainerArrayProperty>();
                        for (var id = 0; id < serverPlayers.Length; id++)
                        {
                            if (id == 1) continue;
                            Container serverPlayer = serverPlayers[id];
                            FloatProperty serverTime = serverPlayer.Require<ServerStampComponent>().time,
                                          localClientTime = serverPlayer.Require<LocalizedClientStampComponent>().time;
                            if (localClientTime.HasValue && serverTime.HasValue)
                            {
                                float previousServerTime = previousServerSession.Require<PlayerContainerArrayProperty>()[id].Require<ServerStampComponent>().time;
                                localClientTime.Value += serverTime - previousServerTime;
                            }
                            else
                                localClientTime.Value = time;
                        }

                        // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                        if (GetLocalPlayerId(out int localPlayerId))
                            CheckPrediction(serverSession.Require<PlayerContainerArrayProperty>()[localPlayerId]);

                        break;
                    }
                }
            });
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}