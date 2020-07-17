using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;
using Voxelfield.Session.Mode;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

#endif

namespace Voxelfield.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        [SerializeField] private int m_ServerPort = DefaultPort;

        private static IPAddress DefaultAddress => IPAddress.Loopback;
        private static int DefaultPort => 7777;
        private static readonly IPEndPoint DefaultEndPoint = new IPEndPoint(DefaultAddress, DefaultPort);
        private static readonly string[] IpSeparator = {":"};

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            ConsoleCommandExecutor.SetCommand("host", args => StartHost());
            ConsoleCommandExecutor.SetCommand("edit", args => StartEdit(args));
            ConsoleCommandExecutor.SetCommand("save", args =>
            {
                string newFile = args.Length > 1 ? args[1] : null;
                if (newFile != null)
                {
                    SessionBase.Sessions.First().GetLatestSession().Require<VoxelMapNameProperty>().SetTo(newFile);
                }
                MapManager.Singleton.SaveCurrentMap(newFile);
            });
            ConsoleCommandExecutor.SetCommand("serve", args => StartServer(DefaultEndPoint));
            ConsoleCommandExecutor.SetCommand("connect", args =>
            {
                try
                {
                    string[] colonSplit = args.Length > 1 ? args[1].Split(IpSeparator, StringSplitOptions.RemoveEmptyEntries) : null;
                    if (colonSplit?.Length == 2) args = args.Take(1).Concat(colonSplit).ToArray();
                    IPAddress address = args.Length > 1 ? IPAddress.Parse(args[1]) : DefaultAddress;
                    int port = args.Length > 2 ? int.Parse(args[2]) : DefaultPort;
                    var endPoint = new IPEndPoint(address, port);

                    Client client = StartClient(endPoint);
                    Debug.Log($"Started client at {client.IpEndPoint}");
                }
                catch (Exception)
                {
                    Debug.LogError("Could not start client with given parameters");
                }
            });
#if ENABLE_MONO
            ConsoleCommandExecutor.SetCommand("wsl_connect", args =>
            {
                var processInfo = new ProcessStartInfo("wsl", "-- hostname -I") {CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true};
                Process process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    string result = process.StandardOutput.ReadLine();
                    ConsoleCommandExecutor.ExecuteCommand($"connect {result}");
                    process.Close();
                }
            });
#endif
            ConsoleCommandExecutor.SetCommand("disconnect", args => DisconnectAll());
            ConsoleCommandExecutor.SetCommand("update_chunks", args =>
            {
                foreach (Chunk chunk in ChunkManager.Singleton.Chunks.Values)
                    chunk.UpdateAndApply();
            });
            ConsoleCommandExecutor.SetCommand("switch_teams", args =>
            {
                if (args.Length > 1 && byte.TryParse(args[1], out byte team))
                    SessionBase.Sessions.First().GetLocalCommands().Require<WantedTeamProperty>().Value = team;
            });

            if (Application.isBatchMode)
            {
                IPEndPoint endPoint = NetUtils.MakeEndPoint(NetUtils.GetLocalIp(LocalAddrType.IPv4), m_ServerPort);
                StartServer(endPoint);
                Debug.Log($"Starting headless server at {endPoint}...");
            }
            ConsoleCommandExecutor.SetCommand("r", args => DebugBehavior.Singleton.RollbackOverrideUs.Value = uint.Parse(args[1]));

            Debug.Log("Started session manager");
            AnalysisLogger.Reset(string.Empty);
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

        private static Host StartEdit(IReadOnlyList<string> args)
        {
            StandaloneDisconnectAll();
            var edit = new Host(VoxelfieldComponents.SessionElements, DefaultEndPoint, new ServerInjector());

            var config = (ConfigManager) ConfigManagerBase.Active;
            if (args.Count > 1) config.mapName.SetTo(args[1]);
            config.modeId.Value = ModeIdProperty.Designer;

            return StartSession(edit);
        }

        private static Host StartHost()
        {
            StandaloneDisconnectAll();
            var host = new Host(VoxelfieldComponents.SessionElements, DefaultEndPoint, new ServerInjector());
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
            var client = new Client(VoxelfieldComponents.SessionElements, ipEndPoint, Application.version, new ClientInjector());
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
            Application.targetFrameRate = ConfigManager.Active.targetFps;
            AudioListener.volume = ConfigManager.Active.volume;
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
                StartServer(DefaultEndPoint);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                StartClient(DefaultEndPoint);
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

        private void OnApplicationQuit()
        {
            DisconnectAll();
            AnalysisLogger.FlushAll();
        }

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
                locationPathName = "Builds/Linux/Voxelfield", target = BuildTarget.StandaloneLinux64, options = BuildOptions.EnableHeadlessMode,
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
                locationPathName = "Builds/Windows/Voxelfield.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.AutoRunPlayer,
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
            // PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "VOXELFIELD_RELEASE");
            BuildWindowsIl2CppPlayer();
            BuildLinuxServer();
        }
#endif
    }
}