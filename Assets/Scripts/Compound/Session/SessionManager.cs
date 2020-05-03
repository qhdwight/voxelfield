using System;
using System.Net;
using Console;
using Swihoni.Sessions;
using Swihoni.Util;
using UnityEngine;

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private NetworkedSessionBase m_Host, m_Client;

        [SerializeField] private SessionGameObjectLinker m_Linker = default;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
            AudioListener.volume = 0.5f;
            ConsoleCommandExecutor.RegisterCommand("host", args => StartHost());
            ConsoleCommandExecutor.RegisterCommand("serve", args => StartServer());
            ConsoleCommandExecutor.RegisterCommand("connect", args =>
            {
                Client client = StartClient(new IPEndPoint(IPAddress.Loopback, 7777));
                Debug.Log($"Started client at {client.IpEndPoint}");
            });
            ConsoleCommandExecutor.RegisterCommand("disconnect", args => DisconnectAll());
        }

        public Host StartHost()
        {
            var host = new Host(m_Linker);
            try
            {
                host.Start();
                m_Host = host;
                return host;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                host.Disconnect();
                return null;
            }
        }
        
        public Server StartServer()
        {
            var host = new Server(m_Linker);
            try
            {
                host.Start();
                m_Host = host;
                return host;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                host.Disconnect();
                return null;
            }
        }

        public Client StartClient(IPEndPoint ipEndPoint)
        {
            var client = new Client(m_Linker, ipEndPoint);
            try
            {
                client.Start();
                m_Client = client;
                return client;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                client.Disconnect();
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
            
            // if (Input.GetKeyDown(KeyCode.H))
            //     StartHost();
            // if (Input.GetKeyDown(KeyCode.J))
            // {
            //     Client client = StartClient(new IPEndPoint(IPAddress.Loopback, 7777));
            //     if (Application.isEditor) client.ShouldRender = false;
            // }
            // if (Input.GetKeyDown(KeyCode.K))
            // {
            //     m_Client?.Disconnect();
            //     m_Client = null;
            // }
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

        private void OnApplicationQuit() => DisconnectAll();
    }
}