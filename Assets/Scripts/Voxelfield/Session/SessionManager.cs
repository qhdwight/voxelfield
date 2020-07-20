using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

#endif

namespace Voxelfield.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private static IPAddress DefaultAddress => IPAddress.Loopback;
        internal static int DefaultPort => 7777;
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

            ConsoleCommandExecutor.SetCommand("rollback_override", args => DebugBehavior.Singleton.RollbackOverrideUs.Value = uint.Parse(args[1]));
            ConsoleCommandExecutor.SetCommand("show_log", args => Application.OpenURL($"file://{Application.consoleLogPath}"));

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

        public static Server StartServer(IPEndPoint ipEndPoint)
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

        private static TSession StartSession<TSession>(TSession session) where TSession : NetworkedSessionBase
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
            Application.targetFrameRate = ConfigManagerBase.Active.targetFps;
            AudioListener.volume = ConfigManagerBase.Active.volume;
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
            if (Input.GetKeyDown(KeyCode.H))
            {
                StartHost();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                StartServer(DefaultEndPoint);
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                StartClient(DefaultEndPoint);
            }
            if (Input.GetKeyDown(KeyCode.K))
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
        [MenuItem("Build/Windows IL2CPP Player")]
        public static void BuildWindowsIl2CppPlayer()
            => Build(ScriptingImplementation.IL2CPP, BuildTarget.StandaloneWindows64, "Windows IL2CPP Player");

        [MenuItem("Build/Windows Mono Player")]
        public static void BuildWindowsMonoPlayer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneWindows64, "Windows Mono Player");

        [MenuItem("Build/Windows IL2CPP Server")]
        public static void BuildWindowsIl2CppServer()
            => Build(ScriptingImplementation.IL2CPP, BuildTarget.StandaloneWindows64, "Windows IL2CPP Server", true);

        [MenuItem("Build/Linux Mono Server")]
        public static void BuildLinuxMonoServer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneLinux64, "Windows Linux Server", true);

        [MenuItem("Build/Release")]
        public static void BuildRelease()
        {
            // PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "VOXELFIELD_RELEASE");
            BuildWindowsIl2CppPlayer();
            BuildLinuxMonoServer();
        }

        private static void Build(ScriptingImplementation scripting, BuildTarget target, string name, bool isServer = false)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, scripting);
            string executablePath = Path.Combine("Builds", name.Replace(' ', Path.DirectorySeparatorChar), "Voxelfield");
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    executablePath = Path.ChangeExtension(executablePath, "exe");
                    break;
                case BuildTarget.StandaloneOSX:
                    executablePath = Path.ChangeExtension(executablePath, "app");
                    break;
            }
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {"Assets/Scenes/Base.unity"},
                locationPathName = executablePath,
                target = target,
                options = isServer ? BuildOptions.EnableHeadlessMode : BuildOptions.AutoRunPlayer
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"{name} build succeeded: {summary.totalSize / 1_000_000:F1} mb");
                    break;
                case BuildResult.Unknown:
                case BuildResult.Failed:
                case BuildResult.Cancelled:
                    Debug.Log($"{name} build result: {summary.result}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#endif
    }
}