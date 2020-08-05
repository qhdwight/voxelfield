#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_CLIENT
#endif

using System;
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
using System.Linq;
using Amazon;

#endif

namespace Voxelfield
{
    public static class GameLiftClientManager
    {
        private const string FleetAlias = "alias-c6bbceef-6fe9-4f60-9949-5db6b26350a1";
        private static readonly BasicAWSCredentials Credentials = new BasicAWSCredentials(@"AKIAWKQVDVRWQMYAUBAV", @"62ixippCgELFUDKgPlGnWqtd0WEZ3w51YhEnMK8C");
        private static readonly AmazonGameLiftConfig Config = new AmazonGameLiftConfig
        {
#if VOXELFIELD_RELEASE_CLIENT
            RegionEndpoint = RegionEndpoint.USWest1

#else
            ServiceURL = $"http://localhost:{SessionManager.DefaultPort}"
#endif
        };
        private static readonly AmazonGameLiftClient GameLiftClient = new AmazonGameLiftClient(Credentials, Config);

        public static string PlayerSessionId { get; private set; } = string.Empty;

        public static async Task<Client> QuickPlayAsync() => await StartClientAsync(GetQuickPlayGameSessionAsync);
        public static async Task<Client> StartNewAsync() => await StartClientAsync(CreateNewGameSessionAsync);

        private static async Task<Client> StartClientAsync(Func<string, Task<GameSession>> sessionGetter)
        {
            try
            {
#if VOXELFIELD_RELEASE_CLIENT
                if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) throw new AuthenticationException("You need to be connected to Steam to play an online game.");
#endif
                string playerId = GetPlayerId();
                GameSession gameSession = await sessionGetter(playerId);
                return await StartClientForGameSessionAsync(gameSession, playerId);
            }
            catch (Exception exception)
            {
                Debug.LogError(Debug.isDebugBuild ? $"Unable to get game session: {exception}" : $"Unable to start online game. Error: {exception.Message}");
                throw;
            }
        }

        private static async Task<Client> StartClientForGameSessionAsync(GameSession gameSession, string playerId)
        {
            Debug.Log("Creating player session request...");
            var playerRequest = new CreatePlayerSessionRequest {GameSessionId = gameSession.GameSessionId, PlayerId = playerId};
            CreatePlayerSessionResponse playerResponse = await GameLiftClient.CreatePlayerSessionAsync(playerRequest);
            PlayerSession playerSession = playerResponse.PlayerSession;
            IPEndPoint endPoint = NetUtils.MakeEndPoint(playerSession.IpAddress, playerSession.Port);
            Debug.Log($"Connecting to server endpoint: {endPoint}");

            PlayerSessionId = playerSession.PlayerSessionId;
            return SessionManager.StartClient(endPoint);
        }

        private static string GetPlayerId()
        {
            string playerId = SteamClient.IsValid && SteamClient.IsLoggedOn
                ? SteamClient.SteamId.ToString()
                : Guid.NewGuid().ToString();
            return playerId;
        }

        private static async Task<GameSession> GetQuickPlayGameSessionAsync(string playerId)
        {
#if VOXELFIELD_RELEASE_CLIENT
            try
            {
                var searchRequest = new SearchGameSessionsRequest
                {
                    Limit = 1,
                    AliasId = FleetAlias,
                    FilterExpression = "hasAvailablePlayerSessions=true",
                    SortExpression = "creationTimeMillis ASC"
                };
                Debug.Log("Searching game sessions...");
                SearchGameSessionsResponse sessionsResponse = await GameLiftClient.SearchGameSessionsAsync(searchRequest);
                return sessionsResponse.GameSessions.Count == 0
                    ? await CreateNewGameSessionAsync(playerId)
                    : sessionsResponse.GameSessions.First();
            }
            catch (InvalidRequestException)
            {
                Debug.LogError("No active online servers found to search for game sessions");
                throw;
            }
#else
            return await CreateNewGameSessionAsync(playerId);
#endif
        }

        private static async Task<GameSession> CreateNewGameSessionAsync(string playerId)
        {
            Debug.Log("No active sessions found. Requesting to start one...");
            var newSessionRequest = new CreateGameSessionRequest
            {
                CreatorId = playerId,
#if VOXELFIELD_RELEASE_CLIENT
                AliasId = FleetAlias,
#else
                FleetId = "fleet-0",
#endif
                MaximumPlayerSessionCount = SessionBase.MaxPlayers
            };
            CreateGameSessionResponse response = await GameLiftClient.CreateGameSessionAsync(newSessionRequest);
            return response.GameSession;
        }
    }
}