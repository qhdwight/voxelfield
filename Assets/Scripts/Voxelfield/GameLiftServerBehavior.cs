#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
// #undef UNITY_EDITOR
#endif

#if UNITY_EDITOR

#endif
using UnityEngine;
#if VOXELFIELD_RELEASE_SERVER
using System.Collections.Generic;
using System.Linq;
using GameSession = Aws.GameLift.Server.Model.GameSession;
using Aws.GameLift;
using Aws.GameLift.Server;
using Voxelfield.Session;

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
    }
}