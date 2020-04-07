using System;
using Session;
using UnityEngine;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>, IGameObjectLinker
    {
        private SessionBase<SessionComponent> m_Session;
        private SessionBase<SessionComponent> m_DebugSession = default;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private void StartSession()
        {
            try
            {
                if (Application.isEditor)
                {
                    m_Session = new Host(this);
                    // m_DebugSession = new Client(this);
                }
                else
                {
                    m_Session = new Client(this);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                m_Session = null;
            }

            m_Session?.Start();
            
            m_DebugSession?.Start();
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

        private void OnDestroy()
        {
            m_Session?.Dispose();
        }

        public (GameObject, GameObject) GetPlayerPrefabs()
        {
            return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab);
        }
    }
}