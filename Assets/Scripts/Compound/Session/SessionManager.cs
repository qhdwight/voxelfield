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
                Disconnect();
                return string.Empty;
            });
        }

        public Host StartHost()
        {
            var host = new Host(this);
            host.Start();
            m_Host = host;
            return host;
        }
        
        public Client StartClient(IPEndPoint ipEndPoint)
        {
            var client = new Client(this, ipEndPoint);
            client.Start();
            m_Client = client;
            return client;
        }

        public void Disconnect()
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
                m_Host = m_Client = null;
            }

            if (Input.GetKeyDown(KeyCode.H))
                StartHost();
            if (Input.GetKeyDown(KeyCode.J))
            {
                Client client = StartClient(new IPEndPoint(IPAddress.Loopback, 7777));
                client.ShouldRender = false;
            }
            if (Input.GetKeyDown(KeyCode.K))
                Disconnect();
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
                m_Host = m_Client = null;
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public (GameObject, GameObject) GetPlayerPrefabs() { return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab); }
    }
}