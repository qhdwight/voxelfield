using System;
using Session;
using UnityEngine;
using Util;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>, IGameObjectLinker
    {
        private SessionBase m_Session;
        private SessionBase m_DebugSession = default;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private void StartSession()
        {
            try
            {
                if (Application.isEditor)
                {
                    // m_Session = new Host(this);
                    m_Session = new Client(this);
                    // m_DebugSession = new Client(this) {ShouldRender = false};
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
            try
            {
                m_DebugSession?.Update();

                m_Session?.Update();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                m_Session = null;
            }
        }

        private void FixedUpdate()
        {
            try
            {
                m_Session?.FixedUpdate();

                m_DebugSession?.FixedUpdate();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                m_Session = null;
            }
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