#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Net;
using Amazon;
using Amazon.DynamoDBv2.Model;
using LiteNetLib;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;
#if VOXELFIELD_RELEASE_SERVER
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
#if VOXELFIELD_RELEASE_SERVER
        private static readonly BasicAWSCredentials DynamoCredentials = new BasicAWSCredentials(@"AKIAWKQVDVRW5MFWZJMG", @"rPxMDaGjpBiaJ9OuT/he5XU4g6rft8ykzXJDgLYP");
        private static readonly AmazonDynamoDBConfig DynamoConfig = new AmazonDynamoDBConfig {RegionEndpoint = RegionEndpoint.USWest1};
        private static readonly AmazonDynamoDBClient DynamoClient = new AmazonDynamoDBClient(DynamoCredentials, DynamoConfig);

        private readonly DualDictionary<NetPeer, string> m_GameLiftPlayerSessionIds = new DualDictionary<NetPeer, string>();
#endif

        private readonly VoxelChangesProperty m_MasterChanges = new VoxelChangesProperty();
        private readonly DualDictionary<NetPeer, SteamId> m_SteamPlayerIds = new DualDictionary<NetPeer, SteamId>();

        public override void EvaluateVoxelChange(in Position3Int worldPosition, in VoxelChange change, Chunk chunk = null, bool updateMesh = true)
        {
            if (MapManager.Singleton.Models.ContainsKey(worldPosition)) return;
            var changed = Session.GetLatestSession().Require<VoxelChangesProperty>();
            base.EvaluateVoxelChange(worldPosition, change, chunk, updateMesh);
            changed.Set(worldPosition, change);
            m_MasterChanges.AddAllFrom(changed);
        }

        public override void VoxelTransaction(EvaluatedVoxelsTransaction uncommitted)
        {
            var changed = Session.GetLatestSession().Require<VoxelChangesProperty>();
            foreach (KeyValuePair<Position3Int, VoxelChange> pair in uncommitted.Map)
                changed.Set(pair.Key, pair.Value);
            m_MasterChanges.AddAllFrom(changed);
            base.VoxelTransaction(uncommitted); // Commit
        }

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<VoxelChangesProperty>();
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
            string version = request.version.AsNewString(),
                   playerSessionId = request.gameLiftPlayerSessionId.AsNewString();
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
                player.Require<SteamIdProperty>().SetTo(request.steamPlayerId);
#if VOXELFIELD_RELEASE_SERVER
                m_GameLiftPlayerSessionIds.Add(peer, request.gameLiftPlayerSessionId.AsNewString());
#endif
                m_SteamPlayerIds.Add(peer, request.steamPlayerId.Value);
            }

#if VOXELFIELD_RELEASE_SERVER
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

        public override void OnSetupHost(in ModifyContext context)
        {
            if (SteamClient.IsValid) context.player.Require<SteamIdProperty>().Value = SteamClient.SteamId;
        }

        protected override void OnSettingsTick(Container session)
        {
            if (SteamServer.IsValid) SteamServer.RunCallbacks();
            base.OnSettingsTick(session);
        }

        public override bool ShouldSetupPlayer(Container serverPlayer) => base.ShouldSetupPlayer(serverPlayer) && serverPlayer.Require<SteamIdProperty>().WithValue;

        public override string GetUsername(in ModifyContext context)
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

        public override void OnServerModify(in ModifyContext context, MoveComponent component)
        {
            if (context.player.Require<HealthProperty>().IsDead) return;

            var move = context.player.Require<MoveComponent>();
            if (move.position.WithoutValue || move.type == MoveType.Flying) return;

            Vector3 eyePosition = SessionBase.GetPlayerEyePosition(move);
            float height = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch);
            // TODO:refactor chunk layer mask no magic value
            if (Physics.Raycast(eyePosition, Vector3.down, out RaycastHit hit, height - 0.1f, 1 << 15))
                move.position.Value += new Vector3 {y = height - hit.distance};

            bool hitBackFaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            if (move.groundTick > 0)
            {
                int count = Physics.RaycastNonAlloc(move, Vector3.down, CachedHits, 1.0f);
                bool isOnBackface = count != 0 && CachedHits[0].normal.y < 0.0f;
                if (isOnBackface)
                {
                    var damageContext = new DamageContext(context, context.playerId, context.player, 1, "Suffocation");
                    context.session.GetModifyingMode(context.sessionContainer).InflictDamage(damageContext);   
                }
            }
            Physics.queriesHitBackfaces = hitBackFaces;
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