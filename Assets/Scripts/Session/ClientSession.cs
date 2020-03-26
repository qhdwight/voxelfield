using Compound;
using Serialization;
using Session.Player;

namespace Session
{
    public class ClientSession : SessionBase
    {
        private const int LocalPlayerId = 0;

        private readonly PlayerCommands m_LocalCommands = new PlayerCommands();
        private readonly SessionState m_RenderState = new SessionState();

        private void ReadLocalInputs()
        {
            InputProvider input = InputProvider.Singleton;
            m_LocalCommands.hInput = input.GetAxis(InputType.Right, InputType.Left);
            m_LocalCommands.vInput = input.GetAxis(InputType.Forward, InputType.Backward);
            m_LocalCommands.jumpInput = input.GetInput(InputType.Jump);
            PlayerManager.Singleton. 
            m_LocalCommands.yaw = InputProvider.GetMouseInput(MouseMovement.X);
            m_LocalCommands.mouseY = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        public override void HandleInput()
        {
            ReadLocalInputs();
        }

        public override void Render()
        {
            base.Render();
            Copier.CopyTo(m_States.Peek(), m_RenderState);
            PlayerState localPlayerState = m_RenderState.LocalPlayerState;
            if (localPlayerState != null)
            {
                localPlayerState.pitch = m_LocalCommands.pitch;
                localPlayerState.yaw = m_LocalCommands.yaw;
            }
            PlayerManager.Singleton.Visualize(m_RenderState);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            ReadLocalInputs();
            SessionState lastState = m_States.Peek();
            float lastTickTime = lastState.time;
            SessionState state = m_States.ClaimNext();
            Copier.CopyTo(lastState, state);
            state.tick = m_Tick;
            state.time = time;
            state.duration = time - lastTickTime;
            state.localPlayerId = LocalPlayerId;
            PlayerState playerState = state.playerStates[LocalPlayerId];
            playerState.isAlive = true;
            m_LocalCommands.duration = state.duration;
            PlayerManager.Singleton.Modify(LocalPlayerId, playerState, m_LocalCommands);
        }
    }
}