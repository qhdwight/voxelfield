using System;
using Collections;
using Components;
using Session.Player;

namespace Session
{
    public abstract class ClientBase<TSessionState>
        : SessionBase<TSessionState>
        where TSessionState : SessionStateComponentBase
    {
        private const int LocalPlayerId = 0;

        private readonly PlayerCommands m_Commands = new PlayerCommands();
        private readonly PlayerStateComponent m_TrustedPlayerState = new PlayerStateComponent();
        private readonly TSessionState m_RenderSessionState = Activator.CreateInstance<TSessionState>();
        private readonly CyclicArray<StampedPlayerStateComponent> m_PredictedPlayerStates =
            new CyclicArray<StampedPlayerStateComponent>(250, () => new StampedPlayerStateComponent());

        private void ReadLocalInputs()
        {
            PlayerManager.Singleton.ModifyCommands(LocalPlayerId, m_Commands);
        }

        public override void HandleInput()
        {
            ReadLocalInputs();
            PlayerManager.Singleton.ModifyTrusted(LocalPlayerId, m_TrustedPlayerState, m_Commands);
        }

        protected override void Render(float timeSinceTick)
        {
            m_RenderSessionState.localPlayerId.Value = LocalPlayerId;
            PlayerStateComponent renderState = m_RenderSessionState.playerStates[LocalPlayerId];
            // if (!InterpolateHistoryInto(renderState, m_PredictedPlayerStates, 1.0f / m_Settings.tickRate * 1.2f, timeSinceTick))
            if (!InterpolateHistoryInto(renderState, m_PredictedPlayerStates, DebugBehavior.Singleton.Rollback, timeSinceTick))
                Copier.CopyTo(m_PredictedPlayerStates.Peek().state, renderState);
            Copier.CopyTo(m_TrustedPlayerState, renderState);
            PlayerManager.Singleton.Visualize(m_RenderSessionState);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            ReadLocalInputs();
            StampedPlayerStateComponent lastPredictedState = m_PredictedPlayerStates.Peek();
            float lastTickTime = lastPredictedState.time.OrElse(time);
            StampedPlayerStateComponent predictedState = m_PredictedPlayerStates.ClaimNext();
            Copier.CopyTo(lastPredictedState, predictedState);
            predictedState.tick.Value = m_Tick;
            predictedState.time.Value = time;
            float duration = time - lastTickTime;
            predictedState.duration.Value = duration;
            predictedState.state.health.Value = 100;
            m_Commands.duration = duration;
            Copier.CopyTo(m_TrustedPlayerState, predictedState.state);
            PlayerManager.Singleton.ModifyChecked(LocalPlayerId, predictedState.state, m_Commands);
        }
    }
}