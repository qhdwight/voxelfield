using System;
using System.Collections.Generic;
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
        public Container playerCommandsContainer, trustedPlayerContainer;
        public StampComponent stamp;
    }

    public abstract class ClientBase : SessionBase
    {
        private const int LocalPlayerId = 0;

        private readonly Container m_RenderSessionContainer;
        private readonly ClientCommandsContainer m_PredictedPlayerCommands;
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, List<Type> sessionElements, List<Type> playerElements, List<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_RenderSessionContainer = new Container(sessionElements);
            m_PredictedPlayerCommands = new ClientCommandsContainer
            {
                playerCommandsContainer = new Container(commandElements),
                trustedPlayerContainer = new Container(playerElements)
            };
            m_PredictedPlayerComponents = new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent
            {
                player = new Container(playerElements)
            });
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
            m_Socket.RegisterComponent(typeof(ClientCommandsContainer));
        }

        private void ReadLocalInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands.playerCommandsContainer);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands.trustedPlayerContainer, m_PredictedPlayerCommands.playerCommandsContainer, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.With(out PlayerContainerArrayProperty playersProperty)
             && m_RenderSessionContainer.With(out LocalPlayerProperty localPlayerProperty))
            {
                localPlayerProperty.Value = LocalPlayerId;
                ComponentBase localPlayerRenderComponent = playersProperty[LocalPlayerId];
                // 1.0f / m_Settings.tickRate * 1.2f
                InterpolateHistoryInto(localPlayerRenderComponent,
                                       i => m_PredictedPlayerComponents.Get(i).player, m_PredictedPlayerComponents.Size,
                                       i => m_PredictedPlayerComponents.Get(i).stamp.duration,
                                       DebugBehavior.Singleton.Rollback, timeSinceTick);
                localPlayerRenderComponent.MergeSet(m_PredictedPlayerCommands.trustedPlayerContainer);
                localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                for (var playerId = 0; playerId < playersProperty.Length; playerId++)
                    m_Visuals[playerId].Render(playersProperty[playerId], playerId == LocalPlayerId);
            }
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            ReadLocalInputs();

            StampedPlayerComponent lastPredictedPlayerComponent = m_PredictedPlayerComponents.Peek(),
                                   predictedPlayerComponent = m_PredictedPlayerComponents.ClaimNext();
            predictedPlayerComponent.Reset();
            predictedPlayerComponent.MergeSet(lastPredictedPlayerComponent);
            predictedPlayerComponent.stamp.tick.Value = m_Tick;
            predictedPlayerComponent.stamp.time.Value = time;
            float duration = time - lastPredictedPlayerComponent.stamp.time.OrElse(time);
            predictedPlayerComponent.stamp.duration.Value = duration;

            // Inject trusted component
            predictedPlayerComponent.player.MergeSet(m_PredictedPlayerCommands.trustedPlayerContainer);
            m_PredictedPlayerCommands.stamp.duration.Value = duration;
            m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent.player, m_PredictedPlayerCommands.playerCommandsContainer, duration);

            // Send off commands to server for checking
            m_PredictedPlayerCommands.stamp.tick.Value = tick;
            m_PredictedPlayerCommands.stamp.time.Value = time;
            m_Socket.SendToServer(m_PredictedPlayerCommands);

            DebugBehavior.Singleton.Predicted = predictedPlayerComponent.player;
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}