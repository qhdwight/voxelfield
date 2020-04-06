using Session;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionBase<SessionComponent> m_Session;
        private SessionBase<SessionComponent> m_DebugSession;

        private void StartSession()
        {
            m_Session = new Host();
            m_Session.Start();
            
            m_DebugSession = new Client();
            m_DebugSession.Start();
        }

        private void Start()
        {
            AnalysisLogger.Reset("");
            StartSession();
        }

        private void Update()
        {
            if (m_Session == null) return;
            m_DebugSession.Update();
            
            m_Session.Update();
        }

        private void FixedUpdate()
        {
            m_Session?.FixedUpdate();
            
            m_DebugSession?.FixedUpdate();
        }
    }
}