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
                m_Host = null;
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
                m_Host = null;
                m_Client = null;
            }
        }

        private void OnDestroy()
        {
            m_Host?.Disconnect();
            m_Client?.Disconnect();
        }

        public (GameObject, GameObject) GetPlayerPrefabs() { return (m_PlayerModifierPrefab, m_PlayerVisualsPrefab); }
    }
}