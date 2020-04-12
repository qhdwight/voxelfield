using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;

namespace Session
{
    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer(IEnumerable<Type> types) : base(types)
        {
        }
    }

    public abstract class ClientBase : SessionBase
    {
        private const int LocalPlayerId = 0;

        private readonly Container m_RenderSessionContainer;
        private readonly ClientCommandsContainer m_PredictedPlayerCommands;
        private readonly CyclicArray<Container> m_PredictedPlayerComponents;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_RenderSessionContainer = new Container(sessionElements);
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playerContainers))
                playerContainers.SetAll( () => new Container(playerElements));
            m_PredictedPlayerCommands = new ClientCommandsContainer(playerElements.Concat(commandElements).Append(typeof(StampComponent)));
            m_PredictedPlayerComponents = new CyclicArray<Container>(250, () => new Container(playerElements.Append(typeof(StampComponent))));
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
            m_Socket.RegisterComponent(typeof(ClientCommandsContainer));
        }

        private void ReadLocalInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands, m_PredictedPlayerCommands, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playersProperty)
             && m_RenderSessionContainer.If(out LocalPlayerProperty localPlayerProperty))
            {
                localPlayerProperty.Value = LocalPlayerId;
                ComponentBase localPlayerRenderComponent = playersProperty[LocalPlayerId];
                // 1.0f / m_Settings.tickRate * 1.2f
                InterpolateHistoryInto(localPlayerRenderComponent,
                                       i => m_PredictedPlayerComponents.Get(i), m_PredictedPlayerComponents.Size,
                                       i => m_PredictedPlayerComponents.Get(i).Require<StampComponent>().duration,
                                       DebugBehavior.Singleton.Rollback, timeSinceTick);
                localPlayerRenderComponent.MergeSet(m_PredictedPlayerCommands);
                localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
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
                    var stampComponent = predictedPlayerComponent.Require<StampComponent>();
                    stampComponent.tick.Value = m_Tick;
                    stampComponent.time.Value = time;
                    float duration = time - lastPredictedPlayerComponent.Require<StampComponent>().time.OrElse(time);
                    stampComponent.duration.Value = duration;

                    // Inject trusted component
                    predictedPlayerComponent.MergeSet(m_PredictedPlayerCommands);
                    var commandsStampComponent = m_PredictedPlayerCommands.Require<StampComponent>();
                    commandsStampComponent.duration.Value = duration;
                    m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent, m_PredictedPlayerCommands, duration);

                    // Send off commands to server for checking
                    commandsStampComponent.tick.Value = tick;
                    commandsStampComponent.time.Value = time;
                    m_Socket.SendToServer(m_PredictedPlayerCommands);

                    DebugBehavior.Singleton.Predicted = predictedPlayerComponent;
                }
            }
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}