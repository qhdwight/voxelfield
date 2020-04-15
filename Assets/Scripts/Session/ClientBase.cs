using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;
using Session.Player.Components;
using UnityEngine;

namespace Session
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

    public abstract class ClientBase : NetworkedSessionBase
    {
        private const int LocalPlayerId = 0;

        private readonly Container m_RenderSessionContainer;
        private readonly CyclicArray<ClientCommandsContainer> m_PredictedPlayerCommands;
        private readonly CyclicArray<Container> m_PredictedPlayerContainers;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                             IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_RenderSessionContainer = new Container(sessionElements);
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playerContainers))
                playerContainers.SetAll(() => new Container(playerElements));
            m_PredictedPlayerCommands = new CyclicArray<ClientCommandsContainer>(250, () => m_ClientCommandsContainer.Clone());
            m_PredictedPlayerContainers = new CyclicArray<Container>(250, () =>
            {
                // IEnumerable<Type> predictedElements = playerElements.Except(new[] {typeof(HealthProperty)}).Append(typeof(StampComponent));
                IEnumerable<Type> predictedElements = playerElements.Append(typeof(ClientStampComponent));
                return new Container(predictedElements);
            });
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Loopback, 7777));
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), m_ClientCommandsContainer);
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), m_ServerSessionContainer);
        }

        private void UpdateInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands.Peek());
        }

        public override void Input(float delta)
        {
            UpdateInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands.Peek(), m_PredictedPlayerCommands.Peek(), delta);
        }

        protected override void Render(float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playersProperty)
             && m_RenderSessionContainer.If(out LocalPlayerProperty localPlayerProperty))
            {
                localPlayerProperty.Value = LocalPlayerId;
                for (var playerId = 0; playerId < playersProperty.Length; playerId++)
                {
                    bool isLocalPlayer = playerId == localPlayerProperty;
                    Container playerRenderComponent = playersProperty[playerId];
                    if (isLocalPlayer)
                    {
                        // 1.0f / m_Settings.tickRate * 1.2f
                        // InterpolateHistoryInto(playerRenderComponent,
                        //                        i => m_PredictedPlayerComponents.Get(i), m_PredictedPlayerComponents.Size,
                        //                        i => m_PredictedPlayerComponents.Get(i).Require<StampComponent>().duration,
                        //                        DebugBehavior.Singleton.Rollback, timeSinceTick);
                        Container GetInHistory(int historyIndex) => m_PredictedPlayerContainers.Get(-historyIndex);
                        float rollback = DebugBehavior.Singleton.Rollback;
                        RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, playerRenderComponent, m_PredictedPlayerContainers.Size, GetInHistory);
                        playerRenderComponent.MergeSet(m_PredictedPlayerCommands.Peek());
                        // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                    }
                    else
                    {
                        int copiedPlayerId = playerId;
                        Container GetInHistory(int historyIndex) => m_SessionComponentHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];
                        float rollback = DebugBehavior.Singleton.Rollback * 3;
                        RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, playerRenderComponent, m_SessionComponentHistory.Size, GetInHistory);
                    }
                    m_Visuals[playerId].Render(playerRenderComponent, isLocalPlayer);
                }
            }
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            UpdateInputs();

            Predict(tick, time);

            Receive();
        }

        private void Predict(uint tick, float time)
        {
            // TODO: refactor into common logic of claiming and resetting
            Container previousPredictedPlayer = m_PredictedPlayerContainers.Peek(),
                      predictedPlayer = m_PredictedPlayerContainers.ClaimNext();
            ClientCommandsContainer previousCommand = m_PredictedPlayerCommands.Peek(),
                                    commands = m_PredictedPlayerCommands.ClaimNext();
            if (predictedPlayer.Has<ClientStampComponent>())
            {
                predictedPlayer.Reset();
                predictedPlayer.MergeSet(previousPredictedPlayer);
                commands.Reset();
                commands.MergeSet(previousCommand);
                // if (tick == 0)
                // {
                //     predictedPlayer.Require<HealthProperty>().Value = 100;
                //     PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayer.Require<InventoryComponent>(), ItemId.TestingRifle, 1);
                //     PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayer.Require<InventoryComponent>(), ItemId.TestingRifle, 2);
                // }
                var predictedStamp = predictedPlayer.Require<ClientStampComponent>();
                predictedStamp.tick.Value = tick;
                predictedStamp.time.Value = time;
                float lastTime = previousPredictedPlayer.Require<ClientStampComponent>().time.OrElse(time),
                      duration = time - lastTime;
                predictedStamp.duration.Value = duration;

                // Inject trusted component
                ClientCommandsContainer predictedCommands = m_PredictedPlayerCommands.Peek();
                var predictedCommandsStamp = predictedCommands.Require<StampComponent>();
                predictedCommandsStamp.MergeSet(predictedStamp);
                predictedPlayer.MergeSet(predictedCommands);
                m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayer, predictedCommands, duration);

                // Send off commands to server for checking
                m_Socket.SendToServer(predictedCommands);

                DebugBehavior.Singleton.Predicted = predictedPlayer;
            }
        }

        private void CheckPrediction(Container serverPlayer)
        {
            uint targetTick = serverPlayer.Require<ClientStampComponent>().tick;
            for (var playerHistoryIndex = 0; playerHistoryIndex < m_PredictedPlayerContainers.Size; playerHistoryIndex++)
            {
                {
                    Container predictedPlayer = m_PredictedPlayerContainers.Get(-playerHistoryIndex);
                    if (predictedPlayer.Require<ClientStampComponent>().tick != targetTick) continue;
                    var areEqual = true;
                    Extensions.NavigateZipped((field, e1, e2) =>
                    {
                        switch (e1)
                        {
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
                m_PredictedPlayerContainers.Get(-playerHistoryIndex).Reset();
                m_PredictedPlayerContainers.Get(-playerHistoryIndex).MergeSet(serverPlayer);
                // Replay old commands up until most recent to get back on track
                for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
                {
                    ClientCommandsContainer commands = m_PredictedPlayerCommands.Get(-commandHistoryIndex);
                    Container predictedPlayer = m_PredictedPlayerContainers.Get(-commandHistoryIndex);
                    ClientStampComponent yuh = predictedPlayer.Require<ClientStampComponent>().Clone();
                    predictedPlayer.Reset();
                    predictedPlayer.MergeSet(m_PredictedPlayerContainers.Get(-commandHistoryIndex - 1));
                    predictedPlayer.Require<ClientStampComponent>().MergeSet(yuh);
                    // predictedPlayer.MergeSet(serverPlayer);
                    m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayer, commands, commands.Require<StampComponent>().duration);
                }
                // Extensions.NavigateZipped((field, e1, e2) =>
                // {
                //     if (e1 is PropertyBase p1 && e2 is PropertyBase p2)
                //     {
                //         if (!p1.Equals(p2))
                //         {
                //             
                //         }
                //     }
                // }, predictedPlayerContainer, serverPlayerContainer);
                break;
            }
        }

        private void Receive()
        {
            m_Socket.PollReceived((id, message) =>
            {
                switch (message)
                {
                    case ServerSessionContainer receivedServerSessionContainer:
                    {
                        ServerSessionContainer serverSessionContainer = m_SessionComponentHistory.ClaimNext();
                        serverSessionContainer.MergeSet(receivedServerSessionContainer);

                        CheckPrediction(serverSessionContainer.Require<PlayerContainerArrayProperty>()[1]);

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