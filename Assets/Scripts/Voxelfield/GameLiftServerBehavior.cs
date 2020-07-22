#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_SERVER
#endif

using UnityEngine;
#if VOXELFIELD_RELEASE_SERVER
using System;
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
                Debug.Log($"{separator}{Environment.NewLine}{message}{Environment.NewLine}{separator}");
            }
            else
            {
                string message = $"Failed to initialize server SDK {outcome.Error}",
                       separator = string.Concat(Enumerable.Repeat("=", message.Length));
                Debug.LogError($"{separator}{Environment.NewLine}{message}{Environment.NewLine}{separator}");
            }
        }

        private static void OnProcessTerminate() => Debug.Log("Terminated game session");

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
    }
}