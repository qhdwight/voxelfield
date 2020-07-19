using System.Collections.Generic;
using System.Net;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using LiteNetLib;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield
{
    [DefaultExecutionOrder(100)]
    public class GameLiftServerBehavior : MonoBehaviour
    {
        [SerializeField] private int m_ServerPort = SessionManager.DefaultPort;

        private void Start()
        {
            // if (!Application.isBatchMode) return;
            GenericOutcome outcome = GameLiftServerAPI.InitSDK();
            if (!outcome.Success)
            {
                Debug.LogError($"Failed to initialize server SDK {outcome.Error}");
                return;
            }
            var logParameters = new LogParameters(new List<string> {Application.consoleLogPath});
            var processParameters = new ProcessParameters(OnStartGameSession, OnProcessTerminate, OnHealthCheck,
                                                          m_ServerPort, logParameters);
            GameLiftServerAPI.ProcessReady(processParameters);
            Debug.Log("GameLift server process ready");
        }

        private void OnProcessTerminate()
        {
            Debug.Log("Terminated game session");
        }

        private void OnStartGameSession(GameSession session)
        {
            Debug.Log($"Started game session: {SessionToString(session)}");
            IPEndPoint endPoint = NetUtils.MakeEndPoint(NetUtils.GetLocalIp(LocalAddrType.IPv4), m_ServerPort);
            SessionManager.StartServer(endPoint);
            Debug.Log($"Started host on private IP: {endPoint}");
        }

        private static string SessionToString(GameSession session)
            => $"{session.IpAddress}:{session.Port} Fleet ID: {session.FleetId} Session ID: {session.GameSessionId}";

        private bool OnHealthCheck() => true;

        private void OnApplicationQuit()
        {
            if (!Application.isBatchMode) return;
            GameLiftServerAPI.Destroy();
        }
    }
}