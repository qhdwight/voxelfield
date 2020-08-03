#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;
#if VOXELFIELD_RELEASE_SERVER
using Swihoni.Sessions.Modes;
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
    public class SteamIdProperty : ULongProperty
    {
        public Friend AsFriend => new Friend(Value);
    }

    [Serializable]
    public class SteamAuthenticationTicketProperty : StringProperty
    {
        public SteamAuthenticationTicketProperty() : base(512) { }
    }

    public class ServerInjector : Injector
    {
        private bool AuthenticateSteam = false;

#if VOXELFIELD_RELEASE_SERVER
        private static readonly BasicAWSCredentials DynamoCredentials = new BasicAWSCredentials(@"AKIAWKQVDVRW5MFWZJMG", @"rPxMDaGjpBiaJ9OuT/he5XU4g6rft8ykzXJDgLYP");
        private static readonly AmazonDynamoDBConfig DynamoConfig = new AmazonDynamoDBConfig {RegionEndpoint = RegionEndpoint.USWest1};
        private static readonly AmazonDynamoDBClient DynamoClient = new AmazonDynamoDBClient(DynamoCredentials, DynamoConfig);

        private readonly DualDictionary<NetPeer, string> m_GameLiftPlayerSessionIds = new DualDictionary<NetPeer, string>();
#endif

        private readonly OrderedVoxelChangesProperty m_MasterChanges = new OrderedVoxelChangesProperty();
        private readonly DualDictionary<NetPeer, SteamId> m_SteamPlayerIds = new DualDictionary<NetPeer, SteamId>();

        public void ApplyVoxelChanges(VoxelChange change, TouchedChunks touchedChunks = null, bool overrideBreakable = false)
        {
            if (!change.position.HasValue) throw new ArgumentException("Voxel change does not have position!");
            if (MapManager.Singleton.Models.ContainsKey(change.position.Value)) return;

            var changed = Session.GetLatestSession().Require<OrderedVoxelChangesProperty>();
            change.undo = new List<Voxel>((int) change.magnitude.GetValueOrDefault(1).Square());
            ChunkManager.Singleton.ApplyVoxelChanges(change, true, touchedChunks, overrideBreakable);
            changed.Add(change);
            m_MasterChanges.Add(change);
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

            if (SteamServer.IsValid)
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

        public override void OnPlayerRegisterAppend(Container player) => player.RegisterAppend(typeof(SteamIdProperty));

        protected override void OnServerNewConnection(ConnectionRequest socketRequest)
        {
            RequestConnectionComponent request = m_RequestConnection;
            void Reject(string message)
            {
                m_RejectionWriter.Reset();
                m_RejectionWriter.Put(message);
                socketRequest.Reject(m_RejectionWriter);
            }
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

            byte[] rawAuthenticationToken = Convert.FromBase64String(request.steamAuthenticationToken.AsNewString());
            if (SteamServer.IsValid && !SteamServer.BeginAuthSession(rawAuthenticationToken, request.steamPlayerId.Value))
            {
                Reject("A Steam authentication error occurred");
                return;
            }

            void Accept()
            {
                NetPeer peer = socketRequest.Accept();
                Container player = Session.GetModifyingPayerFromId(Session.GetPeerPlayerId(peer));
#if VOXELFIELD_RELEASE_SERVER
                // TODO:security what if already exists
                m_GameLiftPlayerSessionIds.Add(peer, request.gameLiftPlayerSessionId.AsNewString());
#endif
                if (!SteamServer.IsValid) return;
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

        public override void OnStart()
        {
            if (!AuthenticateSteam) return;
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
                Debug.Log("Successfully initialized Steam server");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to initialize Steam server {exception.Message}");
            }
        }

        private void SteamServerOnOnValidateAuthTicketResponse(SteamId playerSteamId, SteamId serverSteamId, AuthResponse response)
        {
            if (response == AuthResponse.OK)
            {
                if (m_SteamPlayerIds.TryGetReverse(serverSteamId, out NetPeer peer))
                {
                    Session.GetModifyingPayerFromId(Session.GetPeerPlayerId(peer)).Require<SteamIdProperty>().Value = playerSteamId;
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
            if (SteamClient.IsValid) context.player.Require<SteamIdProperty>().Value = SteamClient.SteamId;
        }

        protected override void OnSettingsTick(Container session)
        {
            if (SteamServer.IsValid) SteamServer.RunCallbacks();
            base.OnSettingsTick(session);
        }

        public override bool ShouldSetupPlayer(Container serverPlayer) => base.ShouldSetupPlayer(serverPlayer)
                                                                       && (!SteamServer.IsValid || serverPlayer.Require<SteamIdProperty>().WithValue);

        public override string GetUsername(in SessionContext context)
        {
            try
            {
                string name = context.player.Require<SteamIdProperty>().AsFriend.Name;
                // TODO:security sanitize name?
                return name;
            }
            catch (Exception)
            {
                return base.GetUsername(context);
            }
        }

        private static readonly RaycastHit[] CachedHits = new RaycastHit[2];

        public override void OnServerMove(in SessionContext context, MoveComponent move)
        {
            if (context.player.Require<HealthProperty>().IsDead) return;

            if (move.position.WithoutValue || move.type == MoveType.Flying) return;

            /* If the normal of the contact (of the upwards raycast) is aligned with the upward vector, it is a backface and we are in the terrain */
            Physics.queriesHitBackfaces = true;
            {
                int count = Physics.RaycastNonAlloc(move + new Vector3 {y = 0.1f}, Vector3.up, CachedHits, float.PositiveInfinity, 1 << 15);
                bool isOnBackface = count != 0 && CachedHits[0].normal.y > 0.0f;
                if (isOnBackface)
                {
                    float distance = CachedHits[0].distance;
                    // if (distance > 4.0f)
                    // {
                    //     var damageContext = new DamageContext(context, context.playerId, context.player, 1, "Suffocation");
                    //     context.session.GetModifyingMode(context.sessionContainer).InflictDamage(damageContext);
                    // }
                    // else
                    move.position.Value += new Vector3 {y = distance + 0.05f};
                }
            }
            Physics.queriesHitBackfaces = false;
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

        public override void OnStop()
        {
            base.OnStop();
            if (SteamServer.IsValid) SteamServer.Shutdown();
        }
    }
}