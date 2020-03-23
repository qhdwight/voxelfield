using Compound;
using Session.Player;

namespace Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionStates m_Session;

        private void StartSession()
        {
            m_Session = new SessionStates(250);
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
            var state = new SessionState {localPlayerId = 0};
            var commands = new SessionCommands();
            PlayerManager.Singleton.Modify(state, commands);
            m_Session.Add(state);
        }
    }
}