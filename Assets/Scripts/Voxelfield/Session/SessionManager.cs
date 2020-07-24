#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Steamworks;
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
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
#else
using LiteNetLib;
#endif
#if VOXELFIELD_RELEASE_SERVER
using Aws.GameLift.Server;

#endif

namespace Voxelfield.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private static IPAddress DefaultAddress => IPAddress.Loopback;
        public static int DefaultPort => 27015;
        private static readonly IPEndPoint DefaultEndPoint = new IPEndPoint(DefaultAddress, DefaultPort);
        private static readonly string[] IpSeparator = {":"};

#if VOXELFIELD_RELEASE_SERVER
        public static bool GameLiftReady { get; set; }
        private float m_InactiveServerElapsedSeconds;
#endif

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
                    SessionBase.SessionEnumerable.First().GetLatestSession().Require<VoxelMapNameProperty>().SetTo(newFile);
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
            ConsoleCommandExecutor.SetCommand("wsl_connect", args => ConsoleCommandExecutor.ExecuteCommand($"connect {SessionExtensions.ExecuteProcess("wsl -- hostname -I")}"));
#endif
            ConsoleCommandExecutor.SetCommand("online_quick_play", args => GameLiftClientManager.QuickPlay());
            ConsoleCommandExecutor.SetCommand("online_start_new", args => GameLiftClientManager.StartNew());
            ConsoleCommandExecutor.SetCommand("disconnect", args => DisconnectAll());
#if UNITY_EDITOR
            ConsoleCommandExecutor.SetCommand("disconnect_client", args => SessionBase.SessionEnumerable.First(session => session is Client).Stop());
#endif
            ConsoleCommandExecutor.SetCommand("update_chunks", args =>
            {
                foreach (Chunk chunk in ChunkManager.Singleton.Chunks.Values)
                    chunk.UpdateAndApply();
            });
            ConsoleCommandExecutor.SetCommand("switch_teams", args =>
            {
                if (args.Length > 1 && byte.TryParse(args[1], out byte team))
                    SessionBase.SessionEnumerable.First().GetLocalCommands().Require<WantedTeamProperty>().Value = team;
            });

            ConsoleCommandExecutor.SetCommand("rollback_override", args => DebugBehavior.Singleton.RollbackOverrideUs.Value = uint.Parse(args[1]));
            ConsoleCommandExecutor.SetCommand("open_log", args => Application.OpenURL($"file://{Application.consoleLogPath}"));

            ConsoleCommandExecutor.SetCommand("steam_status", args =>
            {
                if (SteamClient.IsValid) Debug.Log($"Logged in as {SteamClient.Name}, ID: {SteamClient.SteamId}");
                else Debug.LogWarning("Not connected to steam");
            });

            Debug.Log("Started session manager");
            AnalysisLogger.Reset(string.Empty);
        }

#if UNITY_EDITOR
        private void OnApplicationPause(bool pauseStatus)
        {
            foreach (SessionBase session in SessionBase.SessionEnumerable)
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
            var edit = new Host(VoxelfieldComponents.SessionElements, DefaultEndPoint, new ServerInjector());

            var config = (ConfigManager) ConfigManagerBase.Active;
            if (args.Count > 1) config.mapName.SetTo(args[1]);
            config.modeId.Value = ModeIdProperty.Designer;

            return StartSession(edit);
        }

        private static Host StartHost()
        {
            var host = new Host(VoxelfieldComponents.SessionElements, DefaultEndPoint, new ServerInjector());
            return StartSession(host);
        }

        public static Server StartServer(IPEndPoint ipEndPoint)
        {
            var server = new Server(VoxelfieldComponents.SessionElements, ipEndPoint, new ServerInjector());
            return StartSession(server);
        }

        public static Client StartClient(IPEndPoint ipEndPoint)
        {
            var client = new Client(VoxelfieldComponents.SessionElements, ipEndPoint, new ClientInjector());
            return StartSession(client);
        }

        private static TSession StartSession<TSession>(TSession session) where TSession : NetworkedSessionBase
        {
            try
            {
                StandaloneDisconnectAll();
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
            foreach (SessionBase session in SessionBase.SessionEnumerable)
                session.Stop();
        }

        private void Update()
        {
            SessionBase.HandleCursorLockState();
            Application.targetFrameRate = ConfigManagerBase.Active.targetFps;
            AudioListener.volume = ConfigManagerBase.Active.volume;

#if VOXELFIELD_RELEASE_SERVER
            if (GameLiftReady)
            {
#if UNITY_EDITOR
                IPEndPoint endPoint = DefaultEndPoint;
#else
                IPEndPoint endPoint = NetUtils.MakeEndPoint(NetUtils.GetLocalIp(LocalAddrType.IPv4), DefaultPort);
#endif
                StartServer(endPoint);
                Debug.Log($"Started server on private IP: {endPoint}");
                GameLiftReady = false;
            }

            /* Handle timeout */
            const float maxIdleTimeSeconds = 3.0f * 60.0f;
            void AddTime()
            {
                if (Mathf.Approximately(m_InactiveServerElapsedSeconds, 0.0f))
                    Debug.LogWarning($"Stopping server in {maxIdleTimeSeconds} seconds due to inactivity...");
                m_InactiveServerElapsedSeconds += Time.deltaTime;
            }
            try
            {
                if (SessionBase.SessionEnumerable.FirstOrDefault(session => session is Server) is Server server)
                {
                    // TODO:refactor use session and count number of player session id's?
                    int activePlayerSessionCount = server.Socket.NetworkManager.ConnectedPeersCount;
                    if (activePlayerSessionCount == 0) AddTime();
                    else m_InactiveServerElapsedSeconds = 0.0f;
                }
                else AddTime();
            }
            catch (Exception)
            {
                AddTime();
            }
            if (m_InactiveServerElapsedSeconds > maxIdleTimeSeconds)
            {
                DisconnectAll();
                GameLiftServerAPI.ProcessEnding();
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
#endif

            try
            {
                foreach (SessionBase session in SessionBase.SessionEnumerable)
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
                GameLiftClientManager.QuickPlay();
            }
            if (Input.GetKeyDown(KeyCode.L))
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
                foreach (SessionBase session in SessionBase.SessionEnumerable)
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
        [MenuItem("Build/Windows IL2CPP Player", priority = 100)]
        public static void BuildWindowsIl2CppPlayer()
            => Build(ScriptingImplementation.IL2CPP, BuildTarget.StandaloneWindows64, "Debug Windows IL2CPP Player");

        [MenuItem("Build/Windows Mono Player", priority = 100)]
        public static void BuildWindowsMonoPlayer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneWindows64, "Debug Windows Mono Player");

        [MenuItem("Build/Windows IL2CPP Server", priority = 100)]
        public static void BuildWindowsIl2CppServer()
            => Build(ScriptingImplementation.IL2CPP, BuildTarget.StandaloneWindows64, "Debug Windows IL2CPP Server", true);

        [MenuItem("Build/Linux Mono Server", priority = 100)]
        public static void BuildLinuxMonoServer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneLinux64, "Debug Linux Mono Server", true);

        // [MenuItem("Build/Release Windows Player")]
        // private static void BuildWindowsRelease()
        //     => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneWindows64, "Release Windows Mono Player", defines: new[] {"VOXELFIELD_RELEASE_CLIENT"});

        [MenuItem("Build/Release Windows Player", priority = 200)]
        private static void BuildWindowsRelease()
            => Build(ScriptingImplementation.IL2CPP, BuildTarget.StandaloneWindows64, "Release Windows IL2CPP Player", defines: new[] {"VOXELFIELD_RELEASE_CLIENT"});

        [MenuItem("Build/Release Server", priority = 200)]
        private static void BuildReleaseServer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneLinux64, "Release Linux Mono Server", true, new[] {"VOXELFIELD_RELEASE_SERVER"});

        [MenuItem("Build/Release All", priority = 200)]
        public static void BuildRelease()
        {
            BuildWindowsRelease();
            BuildReleaseServer();
        }

        private static void Build(ScriptingImplementation scripting, BuildTarget target, string name, bool isServer = false, string[] defines = null)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, scripting);
            string executablePath = Path.Combine("Builds", name.Replace(' ', Path.DirectorySeparatorChar), Application.productName);
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    executablePath = Path.ChangeExtension(executablePath, "exe");
                    break;
                case BuildTarget.StandaloneOSX:
                    executablePath = Path.ChangeExtension(executablePath, "app");
                    break;
            }
            // PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone,
            //                                         scripting == ScriptingImplementation.Mono2x ? ManagedStrippingLevel.Disabled : ManagedStrippingLevel.Low);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {"Assets/Scenes/Base.unity"},
                locationPathName = executablePath,
                target = target,
                options = isServer ? BuildOptions.EnableHeadlessMode : BuildOptions.AutoRunPlayer
            };
            if (defines != null) buildPlayerOptions.extraScriptingDefines = defines;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"{name} build succeeded: {summary.totalSize / 1_000_000:F1} mb in {summary.totalTime:mm:ss}");
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