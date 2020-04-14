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
using UnityEngine;

namespace Session
{
    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer() { }

        public ClientCommandsContainer(IEnumerable<Type> types) : base(types) { }
    }

    public abstract class ClientBase : SessionBase
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
            m_PredictedPlayerCommands = new ClientCommandsContainer(m_ClientElements);
            m_PredictedPlayerComponents = new CyclicArray<Container>(250, () => new Container(playerElements.Append(typeof(StampComponent))));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Loopback, 7777));
            m_Socket.RegisterMessage(typeof(ClientCommandsContainer), new ClientCommandsContainer(m_ClientElements));
            m_Socket.RegisterMessage(typeof(ServerSessionContainer), new ServerSessionContainer(m_ServerElements));
        }

        private void ReadLocalInputs() { m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands); }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands, m_PredictedPlayerCommands, delta);
        }

        protected override void Render(float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playersProperty)
             && m_RenderSessionContainer.If(out LocalPlayerProperty localPlayerProperty))
            {
                localPlayerProperty.Value = LocalPlayerId;
                Container localPlayerRenderComponent = playersProperty[LocalPlayerId];
                // 1.0f / m_Settings.tickRate * 1.2f
                InterpolateHistoryInto(localPlayerRenderComponent,
                                       i => m_PredictedPlayerComponents.Get(i), m_PredictedPlayerComponents.Size,
                                       i => m_PredictedPlayerComponents.Get(i).Require<StampComponent>().duration,
                                       DebugBehavior.Singleton.Rollback, timeSinceTick);
                localPlayerRenderComponent.MergeSet(m_PredictedPlayerCommands);
                // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                for (var playerId = 0; playerId < playersProperty.Length; playerId++)
                    m_Visuals[playerId].Render(playersProperty[playerId], playerId == LocalPlayerId);
            }
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            ReadLocalInputs();

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

            {
                m_Socket.PollReceived((id, message) =>
                {
                    switch (message)
                    {
                        case ServerSessionContainer serverSessionContainer:
                        {
                            Debug.Log("Received");
                            break;
                        }
                    }
                });
            }
        }

        public override void Dispose() { m_Socket.Dispose(); }
    }
}