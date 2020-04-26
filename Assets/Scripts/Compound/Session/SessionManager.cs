using System;
using System.Net;
using Console;
using Swihoni.Sessions;
using Swihoni.Util;
using UnityEngine;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>, IGameObjectLinker
    {
        private NetworkedSessionBase m_Host, m_Client;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            ConsoleCommandExecutor.RegisterCommand("host", args =>
            {
                StartHost();
                return string.Empty;
            });
            ConsoleCommandExecutor.RegisterCommand("connect", args =>
            {
                Client client = StartClient(new IPEndPoint(IPAddress.Loopback, 7777));
                return $"Started client at {client.IpEndPoint}";
            });
            ConsoleCommandExecutor.RegisterCommand("disconnect", args =>
            {
                DisconnectAll();
                return string.Empty;
            });
        }

        public Host StartHost()
        {
            var host = new Host(this);
            try
            {
                host.Start();
                m_Host = host;
                return host;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                host.Dispose();
                return null;
            }
        }

        public Client StartClient(IPEndPoint ipEndPoint)
        {
            var client = new Client(this, ipEndPoint);
            try
            {
                client.Start();
                m_Client = client;
                return client;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                client.Dispose();
                return null;
            }
        }

        public void DisconnectAll()
        {
            m_Host?.Disconnect();
            m_Client?.Disconnect();
            m_Host = m_Client = null;
        }

        private void Update()
        {
            float time = Time.realtimeSinceStartup;
            try
            {
                m_Host?.Update(time);
                m_Client?.Update(time);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }

            if (Input.GetKeyDown(KeyCode.H))
                StartHost();
            if (Input.GetKeyDown(KeyCode.J))
            {
                Client client = StartClient(new IPEndPoint(IPAddress.Loopback, 7777));
                if (Application.isEditor) client.ShouldRender = false;
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                m_Client?.Disconnect();
                m_Client = null;
            }
        }

        private void FixedUpdate()
        {
            float time = Time.realtimeSinceStartup;
            try
            {
                m_Host?.FixedUpdate(time);
                m_Client?.FixedUpdate(time);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }
        }

        private void OnDestroy() { DisconnectAll(); }

        public (GameObject, GameObject) GetPlayerPrefabs() { return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab); }
    }
}