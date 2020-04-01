using Session;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionBase<SessionState> m_Session;

        private void StartSession()
        {
            m_Session = new Client();
        }

        private void Start()
        {
            StartSession();
        }

        private void Update()
        {
            if (m_Session == null) return;
            m_Session.HandleInput();
            m_Session.Render();
        }

        private void FixedUpdate()
        {
            m_Session?.FixedUpdate();
        }
    }
}