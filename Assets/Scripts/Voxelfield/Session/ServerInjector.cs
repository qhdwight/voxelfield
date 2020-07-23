#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_SERVER
#endif

using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2.Model;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
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
    public class ServerInjector : Injector
    {
#if VOXELFIELD_RELEASE_SERVER
        private class SessionIdProperty : StringProperty
        {
        }

        private class SteamIdProperty : ULongProperty
        {
            public string AsNewString() => Value.ToString();
        }

        private static readonly BasicAWSCredentials DynamoCredentials = new BasicAWSCredentials(@"AKIAWKQVDVRW5MFWZJMG", @"rPxMDaGjpBiaJ9OuT/he5XU4g6rft8ykzXJDgLYP");
        private static readonly AmazonDynamoDBConfig DynamoConfig = new AmazonDynamoDBConfig {RegionEndpoint = RegionEndpoint.USWest1};
        private static readonly AmazonDynamoDBClient DynamoClient = new AmazonDynamoDBClient(DynamoCredentials, DynamoConfig);
#endif

        private readonly ChangedVoxelsProperty m_MasterChanges = new ChangedVoxelsProperty();

        protected internal override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
        {
            if (MapManager.Singleton.Models.ContainsKey(worldPosition)) return;
            var changed = Session.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.SetVoxelData(worldPosition, change, chunk, updateMesh);
            changed.Set(worldPosition, change);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void SetVoxelRadius(in Position3Int worldPosition, float radius,
                                                        bool replaceGrassWithDirt = false, bool destroyBlocks = false, bool isAdditive = false,
                                                        in VoxelChangeData additiveChange = default,
                                                        ChangedVoxelsProperty changedVoxels = null)
        {
            var changed = Session.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.SetVoxelRadius(worldPosition, radius, replaceGrassWithDirt, destroyBlocks, isAdditive, additiveChange, changed);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void VoxelTransaction(VoxelChangeTransaction uncommitted)
        {
            var changed = Session.GetLatestSession().Require<ChangedVoxelsProperty>();
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in uncommitted.Map)
                changed.Set(pair.Key, pair.Value);
            m_MasterChanges.AddAllFrom(changed);
            base.VoxelTransaction(uncommitted); // Commit
        }

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<ChangedVoxelsProperty>();
            changedVoxels.SetTo(m_MasterChanges);
        }

#if VOXELFIELD_RELEASE_SERVER
        public override void OnServerLoseConnection(NetPeer peer, Container player)
        {
            var playerSessionIdProperty = player.Require<SessionIdProperty>();
            if (playerSessionIdProperty.AsNewString(out string playerSessionId))
            {
                GameLiftServerAPI.RemovePlayerSession(playerSessionId);
                Debug.Log($"Removed player with session ID: {playerSessionId}");
                playerSessionIdProperty.Clear();
            }
            else Debug.LogError("Peer did not have player session id!");
        }

        private readonly UpdateItemRequest m_AddKill = new UpdateItemRequest
        {
            TableName = "Player",
            Key = new Dictionary<string, AttributeValue> {["SteamId"] = new AttributeValue()},
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {[":k"] = new AttributeValue {N = "1"}},
            UpdateExpression = "ADD Kills :k"
        };

        private readonly UpdateItemRequest m_AddDeath = new UpdateItemRequest
        {
            TableName = "Player",
            Key = new Dictionary<string, AttributeValue> {["SteamId"] = new AttributeValue()},
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {[":d"] = new AttributeValue {N = "1"}},
            UpdateExpression = "ADD Deaths :d"
        };

        public override void OnKillPlayer(in DamageContext context)
        {
            string steamId = context.inflictingPlayer.Require<SteamIdProperty>().AsNewString();

            m_AddDeath.Key["SteamId"].N = steamId;
            DynamoClient.UpdateItemAsync(m_AddDeath);

            if (context.IsSelfInflicting) return;

            m_AddKill.Key["SteamId"].N = steamId;
            DynamoClient.UpdateItemAsync(m_AddKill);
        }

        public override void OnPlayerRegisterAppend(Container player) => player.RegisterAppend(typeof(SessionIdProperty), typeof(SteamIdProperty));
#endif

        protected override void OnServerNewConnection(ConnectionRequest request)
        {
            void Reject(string message)
            {
                m_RejectionWriter.Reset();
                m_RejectionWriter.Put(message);
                request.Reject(m_RejectionWriter);
            }
            int nextPeerId = ((NetworkedSessionBase) Session).Socket.NetworkManager.PeekNextId();
            if (nextPeerId >= SessionBase.MaxPlayers - 1)
            {
                Reject("Too many players are already connected to the server!");
                return;
            }

            m_RequestConnection.Deserialize(request.Data);
            string version = m_RequestConnection.version.Builder.ToString(),
                   playerSessionId = m_RequestConnection.gameLiftPlayerSessionId.Builder.ToString();
            if (version != Application.version)
            {
                Reject("Your version does not match that of the server.");
                return;
            }

#if VOXELFIELD_RELEASE_SERVER
            GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(playerSessionId);
            if (outcome.Success)
            {
                Debug.Log($"Accepted player session request with ID: {playerSessionId}");
                NetPeer peer = request.Accept();
                Container player = Session.GetModifyingPayerFromId(Session.GetPeerPlayerId(peer));
                player.Require<SessionIdProperty>().SetTo(playerSessionId);
                player.Require<SteamIdProperty>().Value = 76561198105378699ul;
            }
            else
            {
                Debug.LogError($"Failed to accept player session request with ID: {playerSessionId}, {outcome.Error}");
                Reject("An authentication error occurred");
            }
#else
            request.Accept();
#endif
        }
    }
}