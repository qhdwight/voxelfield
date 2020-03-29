using System;
using Serialization;
using Session.Player;

namespace Session
{
    public abstract class ClientBase<TSessionState> : SessionBase<TSessionState> where TSessionState : SessionStateBase
    {
        private const int LocalPlayerId = 0;

        private readonly PlayerCommands m_Commands = new PlayerCommands();
        private readonly PlayerState m_TrustedState = new PlayerState();
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
            m_RenderSessionState.localPlayerId = LocalPlayerId;
            PlayerState predictedState = m_States.Peek().LocalPlayerState,
                        renderState = m_RenderSessionState.LocalPlayerState;
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
            SessionStateBase lastState = m_States.Peek();
            float lastTickTime = lastState.time;
            SessionStateBase state = m_States.ClaimNext();
            Copier.CopyTo(lastState, state);
            state.tick = m_Tick;
            state.time = time;
            state.duration = time - lastTickTime;
            state.localPlayerId = LocalPlayerId;
            PlayerState playerState = state.LocalPlayerState;
            playerState.isAlive = true;
            m_Commands.duration = state.duration;
            PlayerManager.Singleton.ModifyChecked(LocalPlayerId, playerState, m_Commands);
        }
    }
}