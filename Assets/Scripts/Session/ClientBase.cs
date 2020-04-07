using System;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Items.Modifiers;
using Session.Player;
using Session.Player.Components;
using Session.Player.Modifiers;

namespace Session
{
    public abstract class ClientBase<TSessionComponent>
        : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private const int LocalPlayerId = 0;

        private readonly ClientCommandComponent m_LocalCommands = new ClientCommandComponent();
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents =
            new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent());
        private readonly TSessionComponent m_RenderSessionComponent = Activator.CreateInstance<TSessionComponent>();
        private readonly PlayerComponent m_TrustedPlayerComponent = new PlayerComponent();
        private ComponentClientSocket m_Socket;

        protected ClientBase(IGameObjectLinker linker) : base(linker)
        {
        }
        
        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777), TypeToId);
            m_Socket.StartReceiving();
        }

        private void ReadLocalInputs()
        {
            m_Modifier[LocalPlayerId].ModifyCommands(m_LocalCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_Modifier[LocalPlayerId].ModifyTrusted(m_TrustedPlayerComponent, m_LocalCommands);
        }

        protected override void Render(float renderDelta, float timeSinceTick)
        {
            m_RenderSessionComponent.localPlayerId.Value = LocalPlayerId;
            PlayerComponent localPlayerRenderComponent = m_RenderSessionComponent.playerComponents[LocalPlayerId];
            // InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, 1.0f / m_Settings.tickRate * 1.2f, timeSinceTick);
            InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, player => player.stamp.duration, DebugBehavior.Singleton.Rollback, timeSinceTick);
            Copier.MergeSet(localPlayerRenderComponent, m_TrustedPlayerComponent);
            Copier.MergeSet(localPlayerRenderComponent, DebugBehavior.Singleton.RenderOverride);
            RenderSessionComponent(m_RenderSessionComponent);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            ReadLocalInputs();

            StampedPlayerComponent lastPredictedPlayerComponent = m_PredictedPlayerComponents.Peek();
            float lastTickTime = lastPredictedPlayerComponent.stamp.time.OrElse(time);
            StampedPlayerComponent predictedPlayerComponent = m_PredictedPlayerComponents.ClaimNext();
            Extensions.Emptify(predictedPlayerComponent);
            Copier.MergeSet(predictedPlayerComponent, lastPredictedPlayerComponent);
            predictedPlayerComponent.stamp.tick.Value = m_Tick;
            predictedPlayerComponent.stamp.time.Value = time;
            float duration = time - lastTickTime;
            predictedPlayerComponent.stamp.duration.Value = duration;
            
            if (tick == 0)
            {
                predictedPlayerComponent.health.Value = 100;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.inventory, ItemId.TestingRifle, 2);
            }
            
            m_LocalCommands.tick.Value = tick;
            m_Socket.SendToServer(m_LocalCommands);

            Copier.MergeSet(predictedPlayerComponent, m_TrustedPlayerComponent);
            m_LocalCommands.duration.Value = duration;
            m_Modifier[LocalPlayerId].ModifyChecked(predictedPlayerComponent, m_LocalCommands);

            DebugBehavior.Singleton.Predicted = predictedPlayerComponent;
        }

        public override void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}