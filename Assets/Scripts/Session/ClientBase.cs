using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;
using Session.Items.Modifiers;
using Session.Player.Components;
using Session.Player.Modifiers;

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
        private readonly ClientCommandsContainer m_PredictedPlayerCommands;
        private readonly CyclicArray<Container> m_PredictedPlayerComponents;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                             IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_RenderSessionContainer = new Container(sessionElements);
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playerContainers))
                playerContainers.SetAll(() => new Container(playerElements));
            m_PredictedPlayerCommands = m_ClientCommandsContainer.Clone();
            m_PredictedPlayerComponents = new CyclicArray<Container>(250, () =>
            {
                // IEnumerable<Type> predictedElements = playerElements.Except(new[] {typeof(HealthProperty)}).Append(typeof(StampComponent));
                IEnumerable<Type> predictedElements = playerElements.Append(typeof(StampComponent));
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
            m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands);
        }

        public override void Input(float delta)
        {
            UpdateInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands, m_PredictedPlayerCommands, delta);
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
                        Container GetInHistory(int historyIndex) => m_PredictedPlayerComponents.Get(-historyIndex);
                        float rollback = DebugBehavior.Singleton.Rollback;
                        RenderInterpolatedPlayer<StampComponent>(renderTime - rollback, playerRenderComponent, m_PredictedPlayerComponents.Size, GetInHistory);
                        playerRenderComponent.MergeSet(m_PredictedPlayerCommands);
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
            Container lastPredictedPlayerComponent = m_PredictedPlayerComponents.Peek(),
                      predictedPlayerComponent = m_PredictedPlayerComponents.ClaimNext();
            if (predictedPlayerComponent.Has<StampComponent>())
            {
                predictedPlayerComponent.Reset();
                predictedPlayerComponent.MergeSet(lastPredictedPlayerComponent);
                if (tick == 0)
                {
                    predictedPlayerComponent.Require<HealthProperty>().Value = 100;
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.Require<InventoryComponent>(), ItemId.TestingRifle, 1);
                    PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.Require<InventoryComponent>(), ItemId.TestingRifle, 2);
                }
                var predictedStampComponent = predictedPlayerComponent.Require<StampComponent>();
                predictedStampComponent.tick.Value = tick;
                predictedStampComponent.time.Value = time;
                float lastTime = lastPredictedPlayerComponent.Require<StampComponent>().time.OrElse(time),
                      duration = time - lastTime;
                predictedStampComponent.duration.Value = duration;

                // Inject trusted component
                var commandsStampComponent = m_PredictedPlayerCommands.Require<StampComponent>();
                commandsStampComponent.MergeSet(predictedStampComponent);
                predictedPlayerComponent.MergeSet(m_PredictedPlayerCommands);
                m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent, m_PredictedPlayerCommands, duration);

                // Send off commands to server for checking
                m_Socket.SendToServer(m_PredictedPlayerCommands);

                DebugBehavior.Singleton.Predicted = predictedPlayerComponent;
            }
        }

        private void Receive()
        {
            m_Socket.PollReceived((id, message) =>
            {
                switch (message)
                {
                    case ServerSessionContainer serverSessionContainer:
                    {
                        ServerSessionContainer sessionContainer = m_SessionComponentHistory.ClaimNext();
                        sessionContainer.MergeSet(serverSessionContainer);

                        // if (m_Tick % 30 == 0)
                        // {
                        //     var serverPlayerComponents = sessionContainer.Require<PlayerContainerArrayProperty>();
                        //     for (var i = 0; i < serverPlayerComponents.Length; i++)
                        //     {
                        //         Debug.Log(i + "," + serverPlayerComponents[i].Require<ServerStampComponent>().time.Value);
                        //     }
                        // }
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