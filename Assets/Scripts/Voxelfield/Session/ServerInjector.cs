#if UNITY_EDITOR
// // #define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiteNetLib;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;

#if VOXELFIELD_RELEASE_SERVER
using Amazon;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Aws.GameLift;
using Aws.GameLift.Server;

#endif

namespace Voxelfield.Session
{
    [Serializable]
    public class GameLiftPlayerSessionIdProperty : StringProperty
    {
    }

    [Serializable]
    public class SteamAuthenticationTicketProperty : StringProperty
    {
        public SteamAuthenticationTicketProperty() : base(512) { }
    }

    public class ServerInjector : Injector
    {
#if VOXELFIELD_RELEASE_SERVER
        private static readonly BasicAWSCredentials DynamoCredentials = new BasicAWSCredentials(@"AKIAWKQVDVRW5MFWZJMG", @"rPxMDaGjpBiaJ9OuT/he5XU4g6rft8ykzXJDgLYP");
        private static readonly AmazonDynamoDBConfig DynamoConfig = new AmazonDynamoDBConfig {RegionEndpoint = RegionEndpoint.USWest1};
        private static readonly AmazonDynamoDBClient DynamoClient = new AmazonDynamoDBClient(DynamoCredentials, DynamoConfig);

        private readonly DualDictionary<NetPeer, string> m_GameLiftPlayerSessionIds = new DualDictionary<NetPeer, string>();
#endif

        private readonly OrderedVoxelChangesProperty m_MasterChanges = new OrderedVoxelChangesProperty();
        private readonly DualDictionary<NetPeer, SteamId> m_SteamPlayerIds = new DualDictionary<NetPeer, SteamId>();

        private bool m_UseSteam;

        public void ApplyVoxelChanges(VoxelChange change, TouchedChunks touchedChunks = null, bool overrideBreakable = false)
        {
            void Apply() => ChunkManager.Singleton.ApplyVoxelChanges(change, true, touchedChunks, overrideBreakable);
            if (change.isUndo)
            {
                if (m_MasterChanges.TryRemoveEnd(out VoxelChange lastChange))
                {
                    change.undo = lastChange.undo;
                    Apply();
                }
            }
            else
            {
                if (!change.position.HasValue) throw new ArgumentException("Voxel change does not have position!");

                var changed = Session.GetLatestSession().Require<OrderedVoxelChangesProperty>();
                changed.Append(change);
                change.undo = new List<(Chunk, Position3Int, Voxel)>((int) change.magnitude.GetValueOrDefault(1).Square());
                m_MasterChanges.Append(change);
                Apply();
            }
        }

        public override void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior)
        {
            var center = (Position3Int) throwableBehavior.transform.position;
            var change = new VoxelChange {position = center, magnitude = throwableBehavior.Radius * -0.4f, replace = true, modifiesBlocks = true, form = VoxelVolumeForm.Spherical};
            ApplyVoxelChanges(change);
        }

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<OrderedVoxelChangesProperty>();
            changedVoxels.SetTo(m_MasterChanges);
        }

        public override void OnServerLoseConnection(NetPeer peer, Container player)
        {
#if VOXELFIELD_RELEASE_SERVER
            if (m_GameLiftPlayerSessionIds.TryGetForward(peer, out string playerSessionId))
            {
                GenericOutcome outcome = GameLiftServerAPI.RemovePlayerSession(playerSessionId);
                if (outcome.Success) Debug.Log($"Removed peer with player session ID: {playerSessionId}");
                else Debug.LogError($"Failed to remove peer with Game Lift player session ID: {playerSessionId}");
                m_GameLiftPlayerSessionIds.Remove(peer);
            }
            else Debug.LogError($"Peer {peer.EndPoint} did not have Game Lift player session id!");
#endif

            if (m_UseSteam)
            {
                if (m_SteamPlayerIds.TryGetForward(peer, out SteamId steamId))
                {
                    try
                    {
                        SteamServer.EndSession(steamId);
                        Debug.Log($"Removed Steam ID: {steamId}");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Failed to remove Steam ID: {steamId} for reason: {exception.Message}");
                    }
                    finally
                    {
                        m_SteamPlayerIds.Remove(peer);
                    }
                }
                else Debug.LogError($"Peer {peer.EndPoint} did not have steam id!");
            }
        }

#if VOXELFIELD_RELEASE_SERVER
        private static UpdateItemRequest GetAdditionRequest(string key) => new UpdateItemRequest
        {
            TableName = "Player",
            Key = new Dictionary<string, AttributeValue> {["SteamId"] = new AttributeValue()},
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {[":i"] = new AttributeValue {N = "1"}},
            UpdateExpression = $"ADD {key} :i"
        };

        private readonly UpdateItemRequest m_AddKill = GetAdditionRequest("Kills"), m_AddDeath = GetAdditionRequest("Deaths");

        public override void OnKillPlayer(in DamageContext context)
        {
            if (!context.InflictingPlayer.Require<SteamIdProperty>().TryWithValue(out ulong steamId)) return;

            var steamIdString = steamId.ToString();

            m_AddDeath.Key["SteamId"].N = steamIdString;
            DynamoClient.UpdateItemAsync(m_AddDeath);

            if (context.IsSelfInflicting) return;

            m_AddKill.Key["SteamId"].N = steamIdString;
            DynamoClient.UpdateItemAsync(m_AddKill);
        }
#endif

        protected override void OnServerNewConnection(ConnectionRequest socketRequest)
        {
            RequestConnectionComponent request = m_RequestConnection;
            void Reject(string message)
            {
                try
                {
                    m_RejectionWriter.Reset();
                    m_RejectionWriter.Put(message);
                    socketRequest.Reject(m_RejectionWriter);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Exception rejecting graciously: {exception.Message}");
                    socketRequest.RejectForce();
                }
            }
            try
            {
                int nextPeerId = ((NetworkedSessionBase) Session).Socket.NetworkManager.PeekNextId();
                if (nextPeerId >= SessionBase.MaxPlayers - 1)
                {
                    Reject("Too many players are already connected to the server!");
                    return;
                }

                request.Deserialize(socketRequest.Data);
                string version = request.version.AsNewString();
                if (version != Application.version)
                {
                    Reject("Your version does not match that of the server.");
                    return;
                }

                if (m_UseSteam)
                {
                    if (request.steamPlayerId.TryWithValue(out ulong playerSteamId))
                    {
                        byte[] rawAuthenticationToken = Convert.FromBase64String(request.steamAuthenticationToken.AsNewString());
                        if (!SteamServer.BeginAuthSession(rawAuthenticationToken, playerSteamId))
                        {
                            Reject("Server Steam authentication error occurred");
                            return;
                        }
                    }
                    else
                    {
                        Reject("Server requires Steam authentication");
                        return;
                    }
                }

                void Accept()
                {
                    NetPeer peer = socketRequest.Accept();
                    Container player = Session.GetModifyingPlayerFromId(Session.GetPeerPlayerId(peer));
#if VOXELFIELD_RELEASE_SERVER
                // TODO:security what if already exists
                m_GameLiftPlayerSessionIds.Add(peer, request.gameLiftPlayerSessionId.AsNewString());
#endif
                    if (!m_UseSteam) return;
                    // TODO:security handle case where steam player with ID already connected
                    player.Require<SteamIdProperty>().SetTo(request.steamPlayerId);
                    m_SteamPlayerIds.Add(peer, request.steamPlayerId.Value);
                }

#if VOXELFIELD_RELEASE_SERVER
            string playerSessionId = request.gameLiftPlayerSessionId.AsNewString();
            GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(playerSessionId);
            if (outcome.Success)
            {
                Debug.Log($"Accepted player session request with ID: {playerSessionId}");
                Accept();
            }
            else
            {
                Debug.LogError($"Failed to accept player session request with ID: {playerSessionId}, {outcome.Error}");
                Reject("A player session authentication error occurred");
            }
#else
                Accept();
#endif
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception on new connection: {exception}");
                Reject("Internal server error");
            }
        }

        public override void OnStart()
        {
            if (!(m_UseSteam = Config.Active.authenticateSteam)) return;
            try
            {
                var server = (Server) Session;
                IPEndPoint serverEndPoint = server.IpEndPoint;
                var parameters = new SteamServerInit
                {
                    DedicatedServer = true,
                    GamePort = (ushort) serverEndPoint.Port, QueryPort = 27016,
                    Secure = true,
                    VersionString = Application.version,
                    GameDescription = Application.productName,
                    IpAddress = serverEndPoint.Address,
                    ModDir = Application.productName,
                    SteamPort = 0
                };
                if (!SteamServer.IsValid)
                {
                    SteamServer.Init(480, parameters, false);
                    SteamServer.LogOnAnonymous();
                }

                SteamServer.OnValidateAuthTicketResponse += SteamServerOnOnValidateAuthTicketResponse;
                const string message = "Successfully initialized Steam server";
                string separator = string.Concat(Enumerable.Repeat("@", message.Length));
                Debug.Log($"{separator}\n{message}\n{separator}");
            }
            catch (Exception exception)
            {
                string message = $"Failed to initialize Steam: {exception.Message}",
                       separator = string.Concat(Enumerable.Repeat("@", message.Length));
                Debug.LogError($"{separator}\n{message}\n{separator}");
            }
        }

        private void SteamServerOnOnValidateAuthTicketResponse(SteamId playerSteamId, SteamId serverSteamId, AuthResponse response)
        {
            if (response == AuthResponse.OK)
            {
                if (m_SteamPlayerIds.TryGetReverse(serverSteamId, out NetPeer peer))
                {
                    Session.GetModifyingPlayerFromId(Session.GetPeerPlayerId(peer)).Require<SteamIdProperty>().Value = playerSteamId;
                    Debug.Log($"Successfully validated {playerSteamId}");
                }
                else Debug.LogError($"Peer not found for Steam ID {playerSteamId}");
                return;
            }
            try
            {
                if (m_SteamPlayerIds.TryGetReverse(serverSteamId, out NetPeer peer))
                {
                    var server = (Server) Session;
                    server.Socket.NetworkManager.DisconnectPeer(peer);
                    m_SteamPlayerIds.Remove(playerSteamId);
                    Debug.LogWarning($"Disconnected Steam ID {playerSteamId} with invalid authentication: {response}");
                }
                else Debug.LogError($"Peer not found for Steam ID {playerSteamId}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to disconnect Steam ID {playerSteamId} with invalid authentication: {response} for reason: {exception.Message}");
            }
        }

        public override void OnSetupHost(in SessionContext context)
        {
            if (m_UseSteam) context.player.Require<SteamIdProperty>().Value = SteamClient.SteamId;
        }

        protected override void OnMapChange() => m_MasterChanges.Zero();

        protected override void OnPreTick(Container session)
        {
            if (m_UseSteam) SteamServer.RunCallbacks();
            base.OnPreTick(session);
        }

        public override bool ShouldSetupPlayer(Container serverPlayer) => base.ShouldSetupPlayer(serverPlayer)
                                                                       && (!m_UseSteam || serverPlayer.Require<SteamIdProperty>().WithValue);

        public override string GetUsername(in SessionContext context) => m_UseSteam ? null : base.GetUsername(context);

        private static readonly RaycastHit[] CachedHits = new RaycastHit[2];

        private static void Suffocate(in SessionContext context, byte damage = 1)
        {
            var damageContext = new DamageContext(context, context.playerId, context.player, damage, "Suffocation");
            context.session.GetModifyingMode(context.sessionContainer).InflictDamage(damageContext);
        }

        public override void OnServerMove(in SessionContext context, MoveComponent move)
        {
            if (context.player.H().IsDead) return;

            if (move.position.WithoutValue || move.type == MoveType.Flying) return;

            /* If the normal of the contact (of the upwards raycast) is aligned with the upward vector, it is a backface and we are in the terrain */
            Physics.queriesHitBackfaces = true;
            {
                for (var f = 0.1f; f < move.GetPlayerHeight(); f++)
                {
                    Vector3 origin = move + new Vector3 {y = f};
                    int count = Physics.RaycastNonAlloc(origin, Vector3.up, CachedHits, float.PositiveInfinity, 1 << 15);
                    if (CachedHits.TryClosest(count, out RaycastHit hit) && hit.normal.y > Mathf.Epsilon)
                    {
                        float distance = hit.distance;
                        move.position.Value += new Vector3 {y = distance + 0.05f};
                        if (f > 1.0f && ChunkManager.Singleton.GetVoxel((Position3Int) origin) is Voxel voxel && !voxel.IsBreathable)
                            Suffocate(context, 75);
                        break;
                    }
                }
            }
            Physics.queriesHitBackfaces = false;

            if (MapManager.Singleton.Map.terrainGeneration.upperBreakableHeight.TryWithValue(out int upperLimit)
             && context.sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.SecureArea
             && context.sessionContainer.Require<SecureAreaComponent>().roundTime.WithValue
             && move.position.Value.y > upperLimit + 5) Suffocate(context);

            // Vector3 normal = context.session.GetPlayerModifier(context.player, context.playerId).Movement.Hit.
            // Debug.Log(normal);
            // if (Vector3.Dot(normal, Vector3.down) > 0.0f)
            // {

            // }

            // Voxel.Voxel? _voxel = ChunkManager.Singleton.GetVoxel((Position3Int) eyePosition);
            // if (!(_voxel is Voxel.Voxel voxel) || voxel.OnlySmooth && voxel.density <= 255 / 2) return;
            //
            // var damageContext = new DamageContext(context, context.playerId, context.player, 1, "Suffocation");
            // context.session.GetModifyingMode(context.sessionContainer).InflictDamage(damageContext);
        }
        
        public override void OnPostTick(Container session) => HandleMapReload(session);

        public override void ModifyPlayer(in SessionContext context)
        {
            if (!context.WithServerStringCommands(out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
            {
                switch (arguments.First())
                {
                    case "reload_map":
                        context.sessionContainer.Require<ReloadMapProperty>().Set();
                        break;
                }
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            if (m_UseSteam) SteamServer.Shutdown();
        }
    }
}