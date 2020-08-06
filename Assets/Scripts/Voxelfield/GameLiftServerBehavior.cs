#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
// #undef UNITY_EDITOR
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxelfield.Session;
#if UNITY_EDITOR
using Amazon;
using Amazon.Runtime;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using UnityEditor;

#endif
#if VOXELFIELD_RELEASE_SERVER
using GameSession = Aws.GameLift.Server.Model.GameSession;
using Aws.GameLift;
using Aws.GameLift.Server;

#endif

namespace Voxelfield
{
    [DefaultExecutionOrder(100)]
    public class GameLiftServerBehavior : MonoBehaviour
    {
#if VOXELFIELD_RELEASE_SERVER
        private void Start()
        {
            // try
            // {
            //     var parameters = new SteamServerInit
            //     {
            //         DedicatedServer = true,
            //         GamePort = 27015, QueryPort = 27016,
            //         Secure = true,
            //         VersionString = Application.version,
            //         GameDescription = Application.productName,
            //         IpAddress = NetUtils.ResolveAddress("127.0.0.1"),
            //         ModDir = Application.productName,
            //         SteamPort = 0
            //     };
            //     if (!SteamServer.IsValid)
            //     {
            //         SteamServer.Init(480, parameters, false);
            //         SteamServer.LogOnAnonymous();
            //     }
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("Successfully initialized Steam server");
            // }
            // catch (Exception exception)
            // {
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.LogError($"Failed to initialize Steam server {exception.Message}");
            // }
            
            GenericOutcome outcome = GameLiftServerAPI.InitSDK();
            if (outcome.Success)
            {
                var logParameters = new LogParameters(new List<string> {Application.consoleLogPath});
                // Note: Callbacks are not invoked from main Unity thread
                var processParameters = new ProcessParameters(OnStartGameSession, OnProcessTerminate, OnHealthCheck,
                                                              SessionManager.DefaultPort, logParameters);
                GameLiftServerAPI.ProcessReady(processParameters);
                const string message = "GameLift server process ready";
                string separator = string.Concat(Enumerable.Repeat("=", message.Length));
                Debug.Log($"{separator}\n{message}\n{separator}");
            }
            else
            {
                string message = $"Failed to initialize server SDK {outcome.Error}",
                       separator = string.Concat(Enumerable.Repeat("=", message.Length));
                Debug.LogError($"{separator}\n{message}\n{separator}");
            }
        }

        private static void OnProcessTerminate()
        {
            Debug.Log("Terminated game session");
            SessionManager.WantsApplicationQuit = true;
        }

        private static void OnStartGameSession(GameSession session)
        {
            GenericOutcome outcome = GameLiftServerAPI.ActivateGameSession();
            if (outcome.Success)
            {
                Debug.Log($"Starting game session: {SessionToString(session)}");
                SessionManager.GameLiftReady = true;
            }
            else Debug.Log($"Error activating game session: {outcome.Error}");
        }

        private static string SessionToString(GameSession session)
            => $"{session.IpAddress}:{session.Port} Fleet ID: {session.FleetId} Session ID: {session.GameSessionId}";

        private static bool OnHealthCheck() => true;

        private void OnApplicationQuit() => GameLiftServerAPI.Destroy();
#else
        private void Start() { }
#endif

#if UNITY_EDITOR
        [MenuItem("Build/Create Game Lift Fleet", priority = 300)]
        private static async void CreateFleet()
        {
            // Debug.Log("Executing upload zsh script...");
            // string uploadResult = SessionExtensions.ExecuteProcess($"{Application.dataPath}/../Builds/gamelift_publish.zsh {Application.version}");
            // Debug.Log($"Upload standard output: {uploadResult}");

            var config = new AmazonGameLiftConfig {RegionEndpoint = RegionEndpoint.USWest1};
            var credentials = new BasicAWSCredentials(@"AKIAWKQVDVRWSC42QICS", @"upFhGo0YCcbw+ljii4btFV7EW0TUz3PXTNgk+tje");
            var client = new AmazonGameLiftClient(credentials, config);
            // var buildRequest = new CreateBuildRequest
            // {
            //     Name = "voxelfield", OperatingSystem = OperatingSystem.AMAZON_LINUX_2, Version = Application.version,
            //     StorageLocation = new S3Location{}
            // };
            // CreateBuildResponse response = client.CreateBuild(buildRequest);
            ListBuildsResponse builds = await client.ListBuildsAsync(new ListBuildsRequest {Limit = 1, Status = BuildStatus.READY});
            Build build = builds.Builds.OrderByDescending(b => b.CreationTime).First();
            string buildId = build.BuildId;
            Debug.Log($"Using build: {build.Name} version: {build.Version} for new fleet");

            string activeFleetId = (await client.ListFleetsAsync(new ListFleetsRequest {Limit = 1})).FleetIds.FirstOrDefault();
            if (activeFleetId == null) Debug.Log("No active fleet to terminate");
            else
            {
                await client.DeleteFleetAsync(activeFleetId);
                Debug.Log("Marked active fleet for termination");
            }

            IpPermission CreateIpPermission(IpProtocol protocol) => new IpPermission
            {
                FromPort = SessionManager.DefaultPort, IpRange = "0.0.0.0/0", Protocol = protocol, ToPort = SessionManager.DefaultPort
            };
            var fleetRequest = new CreateFleetRequest
            {
                Name = $"voxelfield_{build.Version}",
                FleetType = FleetType.ON_DEMAND,
                CertificateConfiguration = new CertificateConfiguration {CertificateType = CertificateType.DISABLED},
                BuildId = buildId,
                EC2InboundPermissions = new List<IpPermission> {CreateIpPermission(IpProtocol.TCP), CreateIpPermission(IpProtocol.UDP)},
                EC2InstanceType = EC2InstanceType.C5Large,
                RuntimeConfiguration = new RuntimeConfiguration
                {
                    GameSessionActivationTimeoutSeconds = 600, MaxConcurrentGameSessionActivations = 1,
                    ServerProcesses = new List<ServerProcess> {new ServerProcess {ConcurrentExecutions = 1, LaunchPath = "/local/game/Voxelfield", Parameters = "-logFile /local/game/server.log"}}
                },
                NewGameSessionProtectionPolicy = ProtectionPolicy.FullProtection
            };
            CreateFleetResponse response = await client.CreateFleetAsync(fleetRequest);
            Debug.Log("Created fleet");

            ListAliasesResponse aliases = await client.ListAliasesAsync(new ListAliasesRequest {Limit = 1, Name = "na"});
            Alias alias = aliases.Aliases.First();
            var routine = new RoutingStrategy {FleetId = response.FleetAttributes.FleetId, Type = RoutingStrategyType.SIMPLE};
            await client.UpdateAliasAsync(new UpdateAliasRequest {AliasId = alias.AliasId, RoutingStrategy = routine});
            Debug.Log("Updated alias");
        }
#endif
    }
}