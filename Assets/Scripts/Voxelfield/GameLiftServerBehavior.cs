// #define VOXELFIELD_RELEASE_SERVER

using UnityEngine;
#if VOXELFIELD_RELEASE_SERVER
using Voxelfield.Session;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using LiteNetLib;

#endif

namespace Voxelfield
{
    [DefaultExecutionOrder(100)]
    public class GameLiftServerBehavior : MonoBehaviour
    {
#if VOXELFIELD_RELEASE_SERVER
        private void Start()
        {
            GenericOutcome outcome = GameLiftServerAPI.InitSDK();
            if (outcome.Success)
            {
                var logParameters = new LogParameters(new List<string> {Application.consoleLogPath});
                var processParameters = new ProcessParameters(OnStartGameSession, OnProcessTerminate, OnHealthCheck,
                                                              SessionManager.DefaultPort, logParameters);
                GameLiftServerAPI.ProcessReady(processParameters);
                const string message = "GameLift server process ready";
                string separator = string.Concat(Enumerable.Repeat("=", message.Length));
                Debug.Log(separator);
                Debug.Log(message);
                Debug.Log(separator);
            }
            else
            {
                string message = $"Failed to initialize server SDK {outcome.Error}",
                       separator = string.Concat(Enumerable.Repeat("=", message.Length));
                Debug.LogError(separator);
                Debug.LogError(message);
                Debug.LogError(separator);
            }
        }

        private void OnProcessTerminate() => Debug.Log("Terminated game session");

        private void OnStartGameSession(GameSession session)
        {
            Debug.Log($"Started game session: {SessionToString(session)}");
            IPEndPoint endPoint = NetUtils.MakeEndPoint(NetUtils.GetLocalIp(LocalAddrType.IPv4), SessionManager.DefaultPort);
            SessionManager.StartServer(endPoint);
            Debug.Log($"Started host on private IP: {endPoint}");
        }

        private static string SessionToString(GameSession session)
            => $"{session.IpAddress}:{session.Port} Fleet ID: {session.FleetId} Session ID: {session.GameSessionId}";

        private bool OnHealthCheck() => true;

        private void OnApplicationQuit() => GameLiftServerAPI.Destroy();
#else
        private void Start() { }
#endif
    }
}