using System;
using System.Collections.Generic;
using System.Net;
using Console;
using Swihoni.Sessions;
using Swihoni.Util;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif

namespace Compound.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        [SerializeField] private SessionGameObjectLinker m_LinkerReference;
        [SerializeField] private bool m_IsServer = default;
        [SerializeField] private int m_ServerPort = 7777;

        private readonly List<NetworkedSessionBase> m_Sessions = new List<NetworkedSessionBase>(1);
        private readonly IPEndPoint m_LocalHost = new IPEndPoint(IPAddress.Loopback, 7777);

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
            AudioListener.volume = 0.5f;
            ConsoleCommandExecutor.RegisterCommand("host", args => StartHost());
            ConsoleCommandExecutor.RegisterCommand("serve", args => StartServer(m_LocalHost));
            ConsoleCommandExecutor.RegisterCommand("connect", args =>
            {
                Client client;
                try
                {
                    client = StartClient(new IPEndPoint(IPAddress.Parse(args[1]), int.Parse(args[2])));
                }
                catch (Exception)
                {
                    client = StartClient(m_LocalHost);
                }
                Debug.Log($"Started client at {client.IpEndPoint}");
            });
            ConsoleCommandExecutor.RegisterCommand("disconnect", args => DisconnectAll());
            if (m_IsServer)
            {
                StartServer(new IPEndPoint(IPAddress.Any, m_ServerPort));
            }
        }

        public Host StartHost()
        {
            var host = new Host();
            try
            {
                host.Start();
                m_Sessions.Add(host);
                return host;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                host.Disconnect();
                return null;
            }
        }

        public Server StartServer(IPEndPoint ipEndPoint)
        {
            var server = new Server(ipEndPoint);
            try
            {
                server.Start();
                m_Sessions.Add(server);
                return server;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                server.Disconnect();
                return null;
            }
        }

        public Client StartClient(IPEndPoint ipEndPoint)
        {
            var client = new Client(ipEndPoint);
            try
            {
                client.Start();
                m_Sessions.Add(client);
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
            foreach (NetworkedSessionBase session in m_Sessions)
                session.Disconnect();
            m_Sessions.Clear();
        }

        private void Update()
        {
            float time = Time.realtimeSinceStartup;
            try
            {
                foreach (NetworkedSessionBase session in m_Sessions)
                    session.Update(time);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                Host host = StartHost();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                StartServer(m_LocalHost);
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                Client client = StartClient(m_LocalHost);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                DisconnectAll();
            }
        }

        private void FixedUpdate()
        {
            float time = Time.realtimeSinceStartup;
            try
            {
                foreach (NetworkedSessionBase session in m_Sessions)
                    session.FixedUpdate(time);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }
        }

        private void OnApplicationQuit() => DisconnectAll();

#if UNITY_EDITOR
        [MenuItem("Build/Build Linux Server")]
        public static void BuildLinuxServer()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {"Assets/Scenes/Base.unity"},
                locationPathName = "Builds/Voxelfield/Linux/Voxelfield", target = BuildTarget.StandaloneLinux64, options = BuildOptions.EnableHeadlessMode,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Server Linux build succeeded: {summary.totalSize} bytes");
                    break;
                case BuildResult.Failed:
                    Debug.Log("Server Linux build failed");
                    break;
            }
        }

        [MenuItem("Build/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {"Assets/Scenes/Base.unity"},
                locationPathName = "Builds/Voxelfield/Windows/Voxelfield.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.AutoRunPlayer,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Windows player build succeeded: {summary.totalSize} bytes");
                    break;
                case BuildResult.Failed:
                    Debug.Log("Windows player build failed");
                    break;
            }
        }
#endif
    }
}