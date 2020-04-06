using System;
using Collections;
using Components;
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

        private readonly PlayerCommandsComponent m_Commands = new PlayerCommandsComponent();
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents =
            new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent());
        private readonly TSessionComponent m_RenderSessionComponent = Activator.CreateInstance<TSessionComponent>();
        private readonly PlayerComponent m_TrustedPlayerComponent = new PlayerComponent();

        private void ReadLocalInputs()
        {
            PlayerManager.Singleton.ModifyCommands(LocalPlayerId, m_Commands);
        }

        public override void HandleInput()
        {
            ReadLocalInputs();
            PlayerManager.Singleton.ModifyTrusted(LocalPlayerId, m_TrustedPlayerComponent, m_Commands);
        }

        protected override void Render(float timeSinceTick)
        {
            m_RenderSessionComponent.localPlayerId.Value = LocalPlayerId;
            PlayerComponent localPlayerRenderComponent = m_RenderSessionComponent.playerComponents[LocalPlayerId];
            // InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, 1.0f / m_Settings.tickRate * 1.2f, timeSinceTick);
            InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, DebugBehavior.Singleton.Rollback, timeSinceTick);
            Copier.CopyTo(m_TrustedPlayerComponent, localPlayerRenderComponent);
            Copier.CopyTo(DebugBehavior.Singleton.RenderOverride, localPlayerRenderComponent);
            PlayerManager.Singleton.Visualize(m_RenderSessionComponent);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            ReadLocalInputs();
            StampedPlayerComponent lastPredictedPlayerComponent = m_PredictedPlayerComponents.Peek();
            float lastTickTime = lastPredictedPlayerComponent.time.OrElse(time);
            StampedPlayerComponent predictedPlayerComponent = m_PredictedPlayerComponents.ClaimNext();
            Copier.CopyTo(lastPredictedPlayerComponent, predictedPlayerComponent);
            predictedPlayerComponent.tick.Value = m_Tick;
            predictedPlayerComponent.time.Value = time;
            float duration = time - lastTickTime;
            predictedPlayerComponent.duration.Value = duration;
            predictedPlayerComponent.component.health.Value = 100;
            if (tick == 0)
            {
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.component.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.component.inventory, ItemId.TestingRifle, 2);
            }
            m_Commands.duration.Value = duration;
            Copier.CopyTo(m_TrustedPlayerComponent, predictedPlayerComponent.component);
            PlayerManager.Singleton.ModifyChecked(LocalPlayerId, predictedPlayerComponent.component, m_Commands);
            DebugBehavior.Singleton.Current = predictedPlayerComponent.component;
        }
    }
}