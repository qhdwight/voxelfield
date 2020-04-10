using System;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Components;

namespace Session
{
    [Serializable]
    public class ClientCommandsComponent : ComponentBase
    {
        public ContainerBase playerCommands, trustedPlayerComponent;
        public StampComponent stamp;
    }

    public abstract class ClientBase<TSessionContainer, TCommandsContainer> : SessionBase<TSessionContainer>
        where TSessionContainer : SessionContainerBase, new()
        where TCommandsContainer : ContainerBase, new()
    {
        private const int LocalPlayerId = 0;

        private readonly ClientCommandsComponent m_LocalClientCommands;
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents;
        private readonly SessionContainerBase<ContainerBase> m_RenderSessionContainer;
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker) : base(linker)
        {
            m_RenderSessionContainer = (SessionContainerBase<ContainerBase>) (object) new TSessionContainer();
            m_LocalClientCommands = new ClientCommandsComponent {playerCommands = new TCommandsContainer(), trustedPlayerComponent = new TSessionContainer()};
            m_PredictedPlayerComponents = new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent
            {
                player = (ContainerBase) Activator.CreateInstance(m_EmptySessionComponent.PlayerType),
            });
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));
        }

        private void ReadLocalInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_LocalClientCommands.playerCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_LocalClientCommands.trustedPlayerComponent, m_LocalClientCommands.playerCommands, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            m_RenderSessionContainer.localPlayerId.Value = LocalPlayerId;
            ComponentBase localPlayerRenderComponent = m_RenderSessionContainer.playerComponents[LocalPlayerId];
            // InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, 1.0f / m_Settings.tickRate * 1.2f, timeSinceTick);
            InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, player => player.stamp.duration, DebugBehavior.Singleton.Rollback, timeSinceTick);
            localPlayerRenderComponent.MergeSet(m_LocalClientCommands.trustedPlayerComponent);
            localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
            RenderSessionComponent(m_RenderSessionContainer);
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
            predictedPlayerComponent.player.MergeSet(m_LocalClientCommands.trustedPlayerComponent);
            m_LocalClientCommands.stamp.duration.Value = duration;
            m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent.player, m_LocalClientCommands.playerCommands, duration);

            // Send off commands to server for checking
            m_LocalClientCommands.stamp.tick.Value = tick;
            m_LocalClientCommands.stamp.time.Value = time;
            m_Socket.SendToServer(m_LocalClientCommands);

            DebugBehavior.Singleton.Predicted = predictedPlayerComponent.player;
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}