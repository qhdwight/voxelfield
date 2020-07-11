using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Console;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;
using Voxelfield.Session.Mode;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

#endif

namespace Voxelfield.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        [SerializeField] private int m_ServerPort = 7777;

        private readonly IPEndPoint m_LocalHost = new IPEndPoint(IPAddress.Loopback, 7777);

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
            AudioListener.volume = 0.5f;
            ConsoleCommandExecutor.SetCommand("host", args => StartHost());
            ConsoleCommandExecutor.SetCommand("edit", args => StartEdit(args[1]));
            ConsoleCommandExecutor.SetCommand("save", args => MapManager.Singleton.SaveCurrentMap());
            ConsoleCommandExecutor.SetCommand("serve", args => StartServer(m_LocalHost));
            ConsoleCommandExecutor.SetCommand("connect", args =>
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
            ConsoleCommandExecutor.SetCommand("disconnect", args => DisconnectAll());

            if (Application.isBatchMode)
            {
                IPEndPoint endPoint = NetUtils.MakeEndPoint(NetUtils.GetLocalIp(LocalAddrType.IPv4), m_ServerPort);
                StartServer(endPoint);
                Debug.Log($"Starting headless server at {endPoint}...");
            }
            ConsoleCommandExecutor.SetCommand("r", args => DebugBehavior.Singleton.RollbackOverrideUs.Value = uint.Parse(args[1]));

            Debug.Log("Started session manager");
        }

#if UNITY_EDITOR
        private void OnApplicationPause(bool pauseStatus)
        {
            foreach (SessionBase session in SessionBase.Sessions)
                session.SetApplicationPauseState(pauseStatus);
        }
#endif

        private static void StandaloneDisconnectAll()
        {
#if !UNITY_EDITOR
            DisconnectAll();
#endif
        }

        private Host StartEdit(string mapName)
        {
            StandaloneDisconnectAll();
            var edit = new Host(VoxelfieldComponents.SessionElements, m_LocalHost, new ServerInjector());
            return StartSession(edit);
        }

        private Host StartHost()
        {
            StandaloneDisconnectAll();
            var host = new Host(VoxelfieldComponents.SessionElements, m_LocalHost, new ServerInjector());
            return StartSession(host);
        }

        private static Server StartServer(IPEndPoint ipEndPoint)
        {
            StandaloneDisconnectAll();
            var server = new Server(VoxelfieldComponents.SessionElements, ipEndPoint, new ServerInjector());
            return StartSession(server);
        }

        private static Client StartClient(IPEndPoint ipEndPoint)
        {
            StandaloneDisconnectAll();
            var client = new Client(VoxelfieldComponents.SessionElements, ipEndPoint, Version.String, new ClientInjector());
            return StartSession(client);
        }

        private static T StartSession<T>(T session) where T : NetworkedSessionBase
        {
            try
            {
                session.Start();
                return session;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                session.Stop();
                return null;
            }
        }

        public static void DisconnectAll()
        {
            foreach (SessionBase session in SessionBase.Sessions.ToArray())
                session.Stop();
        }

        private void Update()
        {
            SessionBase.HandleCursorLockState();
            try
            {
                foreach (SessionBase session in SessionBase.Sessions)
                    session.Update();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }

            if (SessionBase.InterruptingInterface) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                StartHost();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Y))
            {
                StartServer(m_LocalHost);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                StartClient(m_LocalHost);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                DisconnectAll();
            }
        }

        private void FixedUpdate()
        {
            try
            {
                foreach (SessionBase session in SessionBase.Sessions)
                    session.FixedUpdate();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                DisconnectAll();
            }
        }

        private void OnApplicationQuit() => DisconnectAll();

        public static void SaveTestMap()
        {
            var models = new ModelsProperty();
            // void AddSpawn(Position3Int position, byte team) =>
            //     models.Add(position, new Container(new ModelIdProperty(ModelsProperty.Spawn), new TeamProperty(team), new ModeIdProperty(ModeIdProperty.Showdown)));
            // AddSpawn(new Position3Int {x = -10, y = 2}, 0);
            // AddSpawn(new Position3Int {y = 5}, 1);
            // AddSpawn(new Position3Int {x = 10, y = 5}, 2);
            // AddSpawn(new Position3Int {x = 20, y = 5}, 3);
            // for (byte i = 0; i < 9; i++)
            //     models.Add(new Position3Int(i * 2 + 5, 5, 5),
            //                new Container(new ModelIdProperty(ModelsProperty.Cure), new IdProperty(i), new ModeIdProperty(ModeIdProperty.Showdown)));
            models.Set(new Position3Int(32, 5, 32),
                       new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.BlueTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
            models.Set(new Position3Int(32, 5, -32),
                       new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.BlueTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
            models.Set(new Position3Int(-32, 5, 32),
                       new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.RedTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
            models.Set(new Position3Int(-32, 5, -32),
                       new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.RedTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
            var testMap = new MapContainer
            {
                name = new StringProperty("Test"),
                terrainHeight = new IntProperty(7),
                dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-2, 0, -2), upperBound = new Position3IntProperty(2, 1, 2)},
                noise = new NoiseComponent
                {
                    seed = new IntProperty(0),
                    octaves = new ByteProperty(3),
                    lateralScale = new FloatProperty(35.0f),
                    verticalScale = new FloatProperty(1.5f),
                    persistance = new FloatProperty(0.5f),
                    lacunarity = new FloatProperty(0.5f)
                },
                models = models,
                breakableEdges = new BoolProperty(false)
            };
            MapManager.SaveMapSave(testMap);
            Debug.Log("Saved Test Map");
        }

#if UNITY_EDITOR
        [MenuItem("Build/Linux Server")]
        public static void BuildLinuxServer()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
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

        [MenuItem("Build/Windows IL2CPP Player")]
        public static void BuildWindowsIl2CppPlayer()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
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
                    Debug.Log($"Windows IL2CPP player build succeeded: {summary.totalSize} bytes");
                    break;
                case BuildResult.Failed:
                    Debug.Log("Windows IL2CPP player build failed");
                    break;
            }
        }

        [MenuItem("Build/Windows Mono Player")]
        public static void BuildWindowsMonoPlayer()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {"Assets/Scenes/Base.unity"},
                locationPathName = "C:/Users/qhdwi/Desktop/Voxelfield/Voxelfield.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.AutoRunPlayer,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Windows Mono player build succeeded: {summary.totalSize} bytes");
                    break;
                case BuildResult.Failed:
                    Debug.Log("Windows Mono player build failed");
                    break;
            }
        }

        [MenuItem("Build/Git Release")]
        public static void BuildRelease()
        {
            BuildWindowsIl2CppPlayer();
            BuildLinuxServer();
        }
#endif
    }
}