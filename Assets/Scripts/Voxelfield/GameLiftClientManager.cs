#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_CLIENT
#endif

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.Runtime;
using LiteNetLib;
using Steamworks;
using Swihoni.Sessions;
using UnityEngine;
using Voxelfield.Session;
#if VOXELFIELD_RELEASE_CLIENT
using System.Security.Authentication;
using Amazon;

#endif

namespace Voxelfield
{
    public static class GameLiftClientManager
    {
        // private const string FleetAlias = "alias-c6bbceef-6fe9-4f60-9949-5db6b26350a1"; // shaweewoo.codes
        // private static readonly BasicAWSCredentials Credentials = new BasicAWSCredentials(@"AKIAWKQVDVRWQMYAUBAV", @"62ixippCgELFUDKgPlGnWqtd0WEZ3w51YhEnMK8C"); // shaweewoo.codes

        private const string FleetAlias = "alias-860fde8f-5f40-45bb-8f17-2b10a8ed1f34";                                                                          // shaweewoo.jo
        private static readonly BasicAWSCredentials Credentials = new BasicAWSCredentials(@"AKIA354QE4UEBBDFV2G2", @"Az53naLwz6/PxGfCRvOMeNQv1Au1lsCGrpvfXOIB"); // shaweewoo.jo
        private static readonly AmazonGameLiftConfig Config = new AmazonGameLiftConfig
        {
#if VOXELFIELD_RELEASE_CLIENT
            RegionEndpoint = RegionEndpoint.USWest1

#else
            ServiceURL = $"http://localhost:{SessionManager.DefaultPort}"
#endif
        };
        private static readonly AmazonGameLiftClient Client = new AmazonGameLiftClient(Credentials, Config);

        public static string PlayerSessionId { get; private set; } = string.Empty;

        public static async Task<Client> QuickPlayAsync()
        {
#if VOXELFIELD_RELEASE_CLIENT
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) throw new AuthenticationException("You need to be connected to Steam to play an online game.");
#endif
            var searchRequest = new SearchGameSessionsRequest
            {
                Limit = 1,
                AliasId = FleetAlias,
                FilterExpression = "hasAvailablePlayerSessions=true",
                SortExpression = "creationTimeMillis ASC"
            };
            Debug.Log("Searching for existing game sessions...");
            SearchGameSessionsResponse existing = await Client.SearchGameSessionsAsync(searchRequest);
            if (existing.GameSessions.Count > 0)
            {
                Debug.Log("Found existing game session!");
                GameSession session = existing.GameSessions.First();
                return await StartClient(session.GameSessionId);
            }
            return await StartClientAsync(EnterQueueAsync);
        }

        private static async Task<Client> StartClient(string gameSessionId)
        {
            Debug.Log("Creating player session request...");
            var playerRequest = new CreatePlayerSessionRequest {GameSessionId = gameSessionId, PlayerId = GetPlayerId()};
            CreatePlayerSessionResponse playerResponse = await Client.CreatePlayerSessionAsync(playerRequest);
            PlayerSession playerSession = playerResponse.PlayerSession;
            IPEndPoint endPoint = NetUtils.MakeEndPoint(playerSession.IpAddress, playerSession.Port);
            Debug.Log($"Connecting to server endpoint: {endPoint}");

            PlayerSessionId = playerSession.PlayerSessionId;
            return SessionManager.StartClient(endPoint);
        }

        private static async Task<GameSessionPlacement> EnterQueueAsync(string _)
        {
            var request = new StartGameSessionPlacementRequest
            {
                GameSessionQueueName = "us-west-1", PlacementId = Guid.NewGuid().ToString(),
                MaximumPlayerSessionCount = SessionBase.MaxPlayers
            };
            Debug.Log("[Queue] Starting placement");
            StartGameSessionPlacementResponse response = await Client.StartGameSessionPlacementAsync(request);
            return response.GameSessionPlacement;
        }

        private static async Task<Client> StartClientAsync(Func<string, Task<GameSessionPlacement>> sessionGetter)
        {
            try
            {
                string playerId = GetPlayerId();
                GameSessionPlacement gameSession = await sessionGetter(playerId);
                return await StartClientForGameSessionPlacementAsync(gameSession);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Unable to retrieve game session: {exception.Message}");
                throw;
            }
        }

        private static async Task<Client> StartClientForGameSessionPlacementAsync(GameSessionPlacement gameSession)
        {
            var delay = 2.0;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(delay));
                var checkRequest = new DescribeGameSessionPlacementRequest {PlacementId = gameSession.PlacementId};
                DescribeGameSessionPlacementResponse checkResponse = await Client.DescribeGameSessionPlacementAsync(checkRequest);
                gameSession = checkResponse.GameSessionPlacement;
                delay = Math.Min(delay + 2.0, 30.0);
                Debug.Log($"[Queue] Status: {gameSession.Status.Value.ToLower()}");
            } while (gameSession.Status == GameSessionPlacementState.PENDING);

            if (gameSession.Status != GameSessionPlacementState.FULFILLED) throw new Exception($"Error waiting in queue: {gameSession.Status}");

            return await StartClient(gameSession.GameSessionId);
        }

        private static string GetPlayerId() => SteamClient.IsValid && SteamClient.IsLoggedOn
            ? SteamClient.SteamId.ToString()
            : Guid.NewGuid().ToString();

//         private static async Task<GameSession> GetQuickPlayGameSessionAsync(string playerId)
//         {
// #if VOXELFIELD_RELEASE_CLIENT
//             try
//             {
//                 var searchRequest = new SearchGameSessionsRequest
//                 {
//                     Limit = 1,
//                     AliasId = FleetAlias,
//                     FilterExpression = "hasAvailablePlayerSessions=true",
//                     SortExpression = "creationTimeMillis ASC"
//                 };
//                 Debug.Log("Searching game sessions...");
//                 SearchGameSessionsResponse sessionsResponse = await GameLiftClient.SearchGameSessionsAsync(searchRequest);
//                 return sessionsResponse.GameSessions.Count == 0
//                     ? await CreateNewGameSessionAsync(playerId)
//                     : sessionsResponse.GameSessions.First();
//             }
//             catch (InvalidRequestException)
//             {
//                 Debug.LogError("No active online servers found to search for game sessions");
//                 throw;
//             }
// #else
//             return await CreateNewGameSessionAsync(playerId);
// #endif
//         }
//
//         private static async Task<GameSession> CreateNewGameSessionAsync(string playerId)
//         {
//             Debug.Log("No active sessions found. Requesting to start one...");
//             var newSessionRequest = new CreateGameSessionRequest
//             {
//                 CreatorId = playerId,
// #if VOXELFIELD_RELEASE_CLIENT
//                 AliasId = FleetAlias,
// #else
//                 FleetId = "fleet-0",
// #endif
//                 MaximumPlayerSessionCount = SessionBase.MaxPlayers
//             };
//             CreateGameSessionResponse response = await Client.CreateGameSessionAsync(newSessionRequest);
//             return response.GameSession;
//         }
    }
}