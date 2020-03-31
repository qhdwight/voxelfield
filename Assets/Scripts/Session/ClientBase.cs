using System;
using Components;
using Session.Player;

namespace Session
{
    public abstract class ClientBase<TSessionState> : SessionBase<TSessionState> where TSessionState : SessionStateComponentBase
    {
        private const int LocalPlayerId = 0;

        private readonly PlayerCommands m_Commands = new PlayerCommands();
        private readonly PlayerStateComponent m_TrustedState = new PlayerStateComponent();
        private readonly TSessionState m_RenderSessionState = Activator.CreateInstance<TSessionState>();

        private void ReadLocalInputs()
        {
            PlayerManager.Singleton.ModifyCommands(LocalPlayerId, m_Commands);
        }

        public override void HandleInput()
        {
            ReadLocalInputs();
            PlayerManager.Singleton.ModifyTrusted(LocalPlayerId, m_TrustedState, m_Commands);
        }

        public override void Render()
        {
            m_RenderSessionState.localPlayerId.Value = LocalPlayerId;
            PlayerStateComponent predictedState = m_States.Peek().playerStates[LocalPlayerId],
                                 renderState = m_RenderSessionState.playerStates[LocalPlayerId];
            if (predictedState == null || renderState == null) return;
            Copier.CopyTo(predictedState, renderState);
            renderState.yaw = m_TrustedState.yaw;
            renderState.pitch = m_TrustedState.pitch;
            PlayerManager.Singleton.Visualize(m_RenderSessionState);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            ReadLocalInputs();
            SessionStateComponentBase lastState = m_States.Peek();
            float lastTickTime = lastState.time;
            SessionStateComponentBase state = m_States.ClaimNext();
            Copier.CopyTo(lastState, state);
            state.tick.Value = m_Tick;
            state.time.Value = time;
            state.duration.Value = time - lastTickTime;
            state.localPlayerId.Value = LocalPlayerId;
            PlayerStateComponent playerState = state.playerStates[LocalPlayerId];
            playerState.health.Value = 100;
            m_Commands.duration = state.duration;
            PlayerManager.Singleton.ModifyChecked(LocalPlayerId, playerState, m_Commands);
        }
    }
}