using System;
using System.Net;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.Runtime;
using LiteNetLib;
using Steamworks;
using Swihoni.Sessions;
using UnityEngine;
using Voxelfield.Session;

#if !UNITY_EDITOR
using System.Linq;
using Amazon;

#endif

namespace Voxelfield
{
    public static class GameLiftClientBehavior
    {
        private const string FleetAlias = "alias-c6bbceef-6fe9-4f60-9949-5db6b26350a1";
        private static readonly AmazonGameLiftConfig Config = new AmazonGameLiftConfig
        {
            Timeout = TimeSpan.FromMilliseconds(200),
#if UNITY_EDITOR
            ServiceURL = $"http://localhost:{SessionManager.DefaultPort}",
            UseHttp = true
#else
            RegionEndpoint = RegionEndpoint.USWest1
#endif
        };
        private static readonly BasicAWSCredentials Credentials = new BasicAWSCredentials(@"AKIAWKQVDVRWQMYAUBAV", @"62ixippCgELFUDKgPlGnWqtd0WEZ3w51YhEnMK8C");
        public static string PlayerSessionId { get; private set; } = string.Empty;

        public static void StartGameLiftClient()
        {
            try
            {
                var client = new AmazonGameLiftClient(Credentials, Config);

                GameSession gameSession = GetGameSession(client);

                Debug.Log("Creating player session request...");
                string playerId = SteamClient.IsValid && SteamClient.IsLoggedOn
                    ? BitConverter.ToString(SteamUser.GetAuthSessionTicket().Data)
                    : Guid.NewGuid().ToString();
                var playerRequest = new CreatePlayerSessionRequest {GameSessionId = gameSession.GameSessionId, PlayerId = playerId};
                CreatePlayerSessionResponse playerResponse = client.CreatePlayerSession(playerRequest);
                PlayerSession playerSession = playerResponse.PlayerSession;
                IPEndPoint endPoint = NetUtils.MakeEndPoint(playerSession.IpAddress, playerSession.Port);
                Debug.Log($"Server endpoint: {endPoint}. Connecting...");

                PlayerSessionId = playerSession.PlayerSessionId;
                SessionManager.StartClient(endPoint);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                throw;
            }
        }

        private static GameSession GetGameSession(IAmazonGameLift client)
        {
#if UNITY_EDITOR
            return CreateNewGameSession(client);
#else
            var searchRequest = new SearchGameSessionsRequest
            {
                Limit = 1,
                AliasId = FleetAlias,
                FilterExpression = "hasAvailablePlayerSessions=true",
                SortExpression = "creationTimeMillis ASC"
            };
            Debug.Log("Searching game sessions...");
            SearchGameSessionsResponse sessionsResponse = client.SearchGameSessions(searchRequest);

            return sessionsResponse.GameSessions.Count == 0
                ? CreateNewGameSession(client)
                : sessionsResponse.GameSessions.First();
#endif
        }

        private static GameSession CreateNewGameSession(IAmazonGameLift client)
        {
            Debug.Log("No active sessions found. Requesting to start one...");
            var newSessionRequest = new CreateGameSessionRequest
            {
#if UNITY_EDITOR
                FleetId = "fleet-0",
#else
                AliasId = FleetAlias,
#endif
                MaximumPlayerSessionCount = SessionBase.MaxPlayers
            };
            CreateGameSessionResponse response = client.CreateGameSession(newSessionRequest);
            return response.GameSession;
        }
    }
}