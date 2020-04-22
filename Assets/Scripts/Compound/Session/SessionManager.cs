using System;
using Swihoni.Sessions;
using Swihoni.Util;
using UnityEngine;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>, IGameObjectLinker
    {
        private SessionBase m_Session;
        private SessionBase m_ClientSession;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;
        [SerializeField] private bool m_AddClient = false, m_RenderClient = false;

        private void StartSession()
        {
            try
            {
                if (Application.isEditor)
                {
                    m_Session = new Host(this);
                    if (m_AddClient) m_ClientSession = new Client(this);
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
            m_ClientSession?.Start();
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            AnalysisLogger.Reset("");
            StartSession();
        }

        private void Update()
        {
            // if (m_DebugSession == null && Time.realtimeSinceStartup > 1.0f)
            // {
            //     m_DebugSession = new Client(this) {ShouldRender = false};
            //     m_DebugSession.Start();
            // }
            // if (Input.GetKeyDown(KeyCode.P))
            // {
            //     m_DebugSession?.Dispose();
            //     m_DebugSession = null;
            // }
            float time = Time.realtimeSinceStartup;
            try
            {
                if (m_Session != null)
                {
                    m_Session.ShouldRender = !m_RenderClient;
                    m_Session.Update(time);
                }
                if (m_ClientSession != null)
                {
                    m_ClientSession.ShouldRender = m_RenderClient;
                    m_ClientSession.Update(time + 25.0f);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                m_Session = null;
                m_ClientSession = null;
            }
        }

        private void FixedUpdate()
        {
            float time = Time.realtimeSinceStartup;
            try
            {
                m_Session?.FixedUpdate(time);
                m_ClientSession?.FixedUpdate(time + 25.0f);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                m_Session = null;
                m_ClientSession = null;
            }
        }

        private void OnDestroy()
        {
            m_Session?.Dispose();
            m_ClientSession?.Dispose();
        }

        public (GameObject, GameObject) GetPlayerPrefabs()
        {
            return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab);
        }
    }
}