using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;
using Voxels;
using Voxels.Map;
using static Swihoni.Sessions.Config.ConsoleCommandExecutor;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Voxelfield.Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private static IPAddress DefaultAddress => IPAddress.Loopback;
        public static int DefaultPort => 27015;
        private static readonly IPEndPoint DefaultEndPoint = new(DefaultAddress, DefaultPort);
        private static readonly string[] IpSeparator = { ":" };

        public static bool WantsApplicationQuit { get; set; }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            WantsApplicationQuit = false;
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            SetCommand("host", arguments => StartHost(GetEndPoint(arguments)));
            SetCommand("edit", arguments => StartEdit(arguments));
            SetCommand("save", arguments =>
            {
                string newFile = arguments.Length > 1 ? arguments[1] : null;
                SessionBase session = SessionBase.SessionEnumerable.First();
                if (newFile != null)
                    session.GetLatestSession().Require<VoxelMapNameProperty>().SetTo(newFile);
                session.GetMapManager().SaveCurrentMap(newFile);
            });
            SetCommand("map_apply_custom", arguments =>
            {
                // OrderedVoxelChangesProperty c = MapManager.Singleton.Map.voxelChanges, clone = c.Clone();
                // c.Clear();

                // foreach (VoxelChange change in clone.List)
                // {
                //     VoxelChange v = change;
                //     if (v.form.Value == VoxelVolumeForm.Prism && v.texture.GetValueOrDefault(VoxelTexture.Solid) == VoxelTexture.Solid && v.hasBlock.GetValueOrDefault())
                //     {
                //         if (v.upperBound.Value.y > v.position.Value.y)
                //             v.upperBound = v.upperBound.Value + new Position3Int(0, 5, 0);
                //         else
                //             v.position = v.position.Value + new Position3Int(0, 5, 0);
                //     }
                //     c.Append(v);
                // }
            });
            SetCommand("map_remove_singles", arguments =>
            {
                MapManager mapManager = SessionBase.SessionEnumerable.First().GetMapManager();
                OrderedVoxelChangesProperty c = mapManager.Map.voxelChanges, clone = c.Clone();
                c.Clear();

                foreach (VoxelChange voxel in clone.List)
                    if (voxel.form != VoxelVolumeForm.Single)
                        c.Append(voxel);
            });

            SetCommand("serve", arguments =>
            {
                Server server = StartServer(GetEndPoint(arguments));
                Debug.Log($"Started server at {server.IpEndPoint}");
            });
            SetCommand("connect", arguments =>
            {
                Client client = StartClient(GetEndPoint(arguments));
                Debug.Log($"Started client at {client.IpEndPoint}");
            });
#if ENABLE_MONO
            SetCommand("wsl_connect",
                arguments =>
                    ExecuteCommand($"connect {SessionExtensions.ExecuteProcess("wsl -- hostname -I").TrimEnd('\n')}"));
#endif
            SetCommand("disconnect", arguments => DisconnectAll());
#if UNITY_EDITOR
            SetCommand("disconnect_client",
                arguments => SessionBase.SessionEnumerable.First(session => session is Client).Stop());
#endif
            SetCommand("update_chunks", arguments =>
            {
                foreach (Chunk chunk in SessionBase.SessionEnumerable.First().GetChunkManager().Chunks.Values)
                    chunk.UpdateAndApply();
            });

            SetCommand("switch_teams", arguments =>
            {
                if (arguments.Length > 1 && byte.TryParse(arguments[1], out byte team))
                    SessionBase.SessionEnumerable.First().GetLocalCommands().Require<WantedTeamProperty>().Value = team;
            });
            SetCommand("rollback_override",
                arguments => DebugBehavior.Singleton.RollbackOverrideUs.Value = uint.Parse(arguments[1]));
            SetCommand("open_log", arguments => Application.OpenURL($"file://{Application.consoleLogPath}"));
            Debug.Log($"[{GetType().Name}] Started session manager");
            AnalysisLogger.Reset(string.Empty);
        }

        private static IPEndPoint GetEndPoint(string[] arguments)
        {
            IPAddress address;
            if (arguments.Length > 1 && arguments[1] == "-l")
                address = IPAddress.Parse(NetUtils.GetLocalIp(LocalAddrType.IPv4));
            else
            {
                string[] colonSplit = arguments.Length > 1
                    ? arguments[1].Split(IpSeparator, StringSplitOptions.RemoveEmptyEntries)
                    : null;
                if (colonSplit?.Length == 2) arguments = arguments.Take(1).Concat(colonSplit).ToArray();
                address = arguments.Length > 1 ? IPAddress.Parse(arguments[1]) : DefaultAddress;
            }

            int port = arguments.Length > 2 ? int.Parse(arguments[2]) : DefaultPort;
            return new IPEndPoint(address, port);
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

        private static Host StartEdit(IReadOnlyList<string> arguments)
        {
            var edit = new Host(VoxelfieldComponents.SessionElements, DefaultEndPoint, new ServerInjector());
            Config config = Config.Active;
            if (arguments.Count > 1) config.mapName.SetTo(arguments[1]);
            config.modeId.Value = ModeIdProperty.Designer;
            return StartSession(edit);
        }

        public static Host StartHost(IPEndPoint ipEndPoint = null)
        {
            ipEndPoint ??= DefaultEndPoint;
            var host = new Host(VoxelfieldComponents.SessionElements, ipEndPoint, new ServerInjector());
            return StartSession(host);
        }

        public static Server StartServer(IPEndPoint ipEndPoint = null)
        {
            ipEndPoint ??= DefaultEndPoint;
            var server = new Server(VoxelfieldComponents.SessionElements, ipEndPoint, new ServerInjector());
            return StartSession(server);
        }

        public static Client StartClient(IPEndPoint ipEndPoint = null)
        {
            ipEndPoint ??= DefaultEndPoint;
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
                L.Exception(exception, "Error starting session");
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
            if (WantsApplicationQuit)
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }

            SessionBase.HandleCursorLockState();
            if (SessionBase.SessionCount == 0)
            {
                var rate = Screen.currentResolution.refreshRateRatio;
                Application.targetFrameRate = (int)(rate.numerator / rate.denominator + 1);
            }
            else
            {
                int targetFps = DefaultConfig.Active.targetFps;
                if (targetFps is > 0 and < 10) targetFps = 10;
                Application.targetFrameRate = targetFps;
            }

            AudioListener.volume = DefaultConfig.Active.volume;
            try
            {
                foreach (SessionBase session in SessionBase.SessionEnumerable)
                    session.Update();
            }
            catch (Exception exception)
            {
                L.Exception(exception, "Error updating sessions");
                DisconnectAll();
            }

            if (SessionBase.InterruptingInterface) return;
            if (Input.GetKeyDown(KeyCode.H))
            {
                StartHost();
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                StartServer();
            }
            
            if (Input.GetKeyDown(KeyCode.L))
            {
                StartClient();
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
                L.Exception(exception, "Error updating sessions (fixed)");
                DisconnectAll();
            }
        }

        private void OnApplicationQuit()
        {
            DisconnectAll();
            AnalysisLogger.FlushAll();
        }

#if UNITY_EDITOR
        [MenuItem("Voxelfield/Save Custom Map")]
        public static void SaveCustomMap()
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
// models.Set(new Position3Int(32, 5, 32),
//            new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.BlueTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
// models.Set(new Position3Int(32, 5, -32),
//            new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.BlueTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
// models.Set(new Position3Int(-32, 5, 32),
//            new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.RedTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
// models.Set(new Position3Int(-32, 5, -32),
//            new Container(new ModelIdProperty(ModelsProperty.Flag), new TeamProperty(CtfMode.RedTeam), new ModeIdProperty(ModeIdProperty.Ctf)));
            var testMap = new MapContainer
            {
                name = new StringProperty("Fort"),
                terrainHeight = new IntProperty(9),
                dimension = new DimensionComponent
                {
                    lowerBound = new Position3IntProperty(-2, 0, -2), upperBound = new Position3IntProperty(2, 1, 2)
                },
                terrainGeneration = new TerrainGenerationComponent
                {
                    seed = new IntProperty(1337),
                    octaves = new ByteProperty(3),
                    lateralScale = new FloatProperty(35.0f),
                    verticalScale = new FloatProperty(1.5f),
                    persistence = new FloatProperty(0.5f),
                    lacunarity = new FloatProperty(0.5f),
                    grassVoxel = new VoxelChangeProperty(new VoxelChange
                        { texture = VoxelTexture.Solid, color = new Color32(255, 172, 7, 255) }),
                    stoneVoxel = new VoxelChangeProperty(new VoxelChange
                        { texture = VoxelTexture.Checkered, color = new Color32(28, 28, 28, 255) }),
                },
                models = models,
                breakableEdges = new BoolProperty(false)
            };
            MapManager.SaveMapSave(testMap);
            AssetDatabase.Refresh();
            Debug.Log("Saved Custom Map");
        }

        [MenuItem("Build/Mac Universal Mono Player", priority = 100)]
        public static void BuildMacMonoPlayer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneOSX, "Mac Mono Player");

        [MenuItem("Build/Windows Mono Player", priority = 100)]
        public static void BuildWindowsMonoPlayer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneWindows64, "Windows Mono Player");

        [MenuItem("Build/Linux Mono Server", priority = 100)]
        public static void BuildLinuxMonoServer()
            => Build(ScriptingImplementation.Mono2x, BuildTarget.StandaloneLinux64, "Linux Mono Server", true);
        
        private static void Build(ScriptingImplementation scripting, BuildTarget target, string name,
            bool isServer = false, string[] defines = null)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, scripting);
            string executablePath = Path.Combine("Builds", name.Replace(' ', Path.DirectorySeparatorChar),
                Application.productName);
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    executablePath = Path.ChangeExtension(executablePath, "exe");
                    break;
                case BuildTarget.StandaloneOSX:
                    executablePath = Path.ChangeExtension(executablePath, "app");
                    break;
            }

            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone,
                scripting == ScriptingImplementation.Mono2x
                    ? ManagedStrippingLevel.Disabled
                    : ManagedStrippingLevel.Low);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Base.unity" },
                locationPathName = executablePath,
                target = target,
                options = BuildOptions.CompressWithLz4
            };
            EditorUserBuildSettings.standaloneBuildSubtarget =
                isServer ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player;
            if (defines != null) buildPlayerOptions.extraScriptingDefines = defines;
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log(
                        $"{name} build succeeded: {summary.totalSize / 1_000_000} mb in {summary.totalTime:mm\\:ss}");
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