using Session;
using UnityEngine;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>, IGameObjectLinker
    {
        private SessionBase<SessionComponent> m_Session;
        private SessionBase<SessionComponent> m_DebugSession;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private void StartSession()
        {
            m_Session = new Client(this);
            m_Session.Start();

            // m_DebugSession = new Client(this);
            // m_DebugSession.Start();
        }

        private void Start()
        {
            AnalysisLogger.Reset("");
            StartSession();
        }

        private void Update()
        {
            m_DebugSession?.Update();

            m_Session?.Update();
        }

        private void FixedUpdate()
        {
            m_Session?.FixedUpdate();

            m_DebugSession?.FixedUpdate();
        }

        public (GameObject, GameObject) GetPlayerPrefabs()
        {
            return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab);
        }
    }
}