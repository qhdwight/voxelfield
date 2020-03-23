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

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            var state = new SessionState();
            PlayerManager.Singleton.Modify(state);
        }
    }
}