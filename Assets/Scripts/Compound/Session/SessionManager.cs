using Session;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionBase<SessionComponent> m_Session;

        private void StartSession()
        {
            // m_Session = new Client();
            m_Session = new Host();
            m_Session.Start();
        }

        private void Start()
        {
            AnalysisLogger.Reset("");
            StartSession();
        }

        private void Update()
        {
            if (m_Session == null) return;
            m_Session.Update();
        }

        private void FixedUpdate()
        {
            m_Session?.FixedUpdate();
        }
    }
}