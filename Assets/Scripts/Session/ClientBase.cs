using System;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;

namespace Session
{
    using SessionContainer = SessionContainerBase<ContainerBase>;

    [Serializable]
    public class ClientCommandsComponent : ComponentBase
    {
        public ContainerBase playerCommands, trustedPlayerComponent;
        public StampComponent stamp;
    }

    public abstract class ClientBase : SessionBase
    {
        private const int LocalPlayerId = 0;

        private readonly SessionContainer m_RenderSessionContainer;
        private readonly ClientCommandsComponent m_PredictedPlayerCommands;
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker, Type sessionType, Type playerType, Type commandsType) : base(linker, sessionType, playerType, commandsType)
        {
            m_RenderSessionContainer = (SessionContainer) Activator.CreateInstance(sessionType);
            m_PredictedPlayerCommands = new ClientCommandsComponent
            {
                playerCommands = (ContainerBase) Activator.CreateInstance(commandsType),
                trustedPlayerComponent = (ContainerBase) Activator.CreateInstance(playerType)
            };
            m_PredictedPlayerComponents = new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent
            {
                player = (ContainerBase) Activator.CreateInstance(commandsType)
            });
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
            m_Socket.RegisterComponent(typeof(ClientCommandsComponent));
        }

        private void ReadLocalInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_PredictedPlayerCommands.playerCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_PredictedPlayerCommands.trustedPlayerComponent, m_PredictedPlayerCommands.playerCommands, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            m_RenderSessionContainer.localPlayerId.Value = LocalPlayerId;
            ComponentBase localPlayerRenderComponent = m_RenderSessionContainer.playerComponents[LocalPlayerId];
            // 1.0f / m_Settings.tickRate * 1.2f
            InterpolateHistoryInto(localPlayerRenderComponent,
                                   i => m_PredictedPlayerComponents.Get(i).player, m_PredictedPlayerComponents.Size,
                                   i => m_PredictedPlayerComponents.Get(i).stamp.duration,
                                   DebugBehavior.Singleton.Rollback, timeSinceTick);
            localPlayerRenderComponent.MergeSet(m_PredictedPlayerCommands.trustedPlayerComponent);
            localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
            for (var playerId = 0; playerId < m_RenderSessionContainer.playerComponents.Length; playerId++)
                m_Visuals[playerId].Render(m_RenderSessionContainer.playerComponents[playerId], playerId == m_RenderSessionContainer.localPlayerId);
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
            predictedPlayerComponent.player.MergeSet(m_PredictedPlayerCommands.trustedPlayerComponent);
            m_PredictedPlayerCommands.stamp.duration.Value = duration;
            m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent.player, m_PredictedPlayerCommands.playerCommands, duration);

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