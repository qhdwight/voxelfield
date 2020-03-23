using Compound;
using Session.Player;
using UnityEngine;

namespace Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionStates m_Session;

        private void StartSession()
        {
            m_Session = new SessionStates(250);
            Time.fixedDeltaTime = 1.0f / 60.0f;
        }

        protected override void Awake()
        {
            base.Awake();
            StartSession();
        }

        private void Update()
        {
            if (m_Session == null) return;
            PlayerManager.Singleton.Visualize(m_Session.Peek());
        }

        private void FixedUpdate()
        {
            if (m_Session == null) return;
            float currentTime = Time.realtimeSinceStartup;
            var state = new SessionState {localPlayerId = 0, time = currentTime, duration = currentTime - m_Session.Peek()?.time ?? 0.0f, tick = m_Session.Peek()?.tick ?? 0 + 1};
            var commands = new SessionCommands();

            PlayerCommands localCommands = commands.playerCommands[0];
            localCommands.duration = state.duration;
            InputProvider input = InputProvider.Singleton;
            localCommands.hInput = input.GetAxis(InputType.Right, InputType.Left);
            localCommands.vInput = input.GetAxis(InputType.Forward, InputType.Backward);
            localCommands.jumpInput = input.GetInput(InputType.Jump);
            localCommands.mouseX = InputProvider.GetMouseInput(MouseMovement.X);
            localCommands.mouseY = InputProvider.GetMouseInput(MouseMovement.Y);

            PlayerManager.Singleton.Modify(state, commands);
            m_Session.Add(state);
        }
    }
}