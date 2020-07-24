using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;

namespace Voxelfield.Session
{
    public class ClientInjector : Injector
    {
        private readonly Pool<VoxelChangesProperty> m_ChangesPool = new Pool<VoxelChangesProperty>(1, () => new EvaluatedVoxelsTransaction());
        private readonly UIntProperty m_Pointer = new UIntProperty();
        private readonly SortedDictionary<uint, VoxelChangesProperty> m_Changes = new SortedDictionary<uint, VoxelChangesProperty>();
        private readonly EvaluatedVoxelsTransaction m_Transaction = new EvaluatedVoxelsTransaction();

        public override void EvaluateVoxelChange(in Position3Int worldPosition, in VoxelChange change, Chunk chunk = null, bool updateMesh = true) { }

        public override void VoxelTransaction(EvaluatedVoxelsTransaction uncommitted) { }

        public override NetDataWriter GetConnectWriter()
        {
            var writer = new NetDataWriter();
            RequestConnectionComponent request = m_RequestConnection;
            request.version.SetTo(Application.version);
            if (TryGetSteamAuthTicket())
            {
                request.steamAuthenticationToken.SetTo(Convert.ToBase64String(m_SteamAuthenticationTicket.Data));
                request.steamPlayerId.Value = SteamClient.SteamId;
            }
            request.gameLiftPlayerSessionId.SetTo(GameLiftClientManager.PlayerSessionId);
            request.Serialize(writer);
            return writer;
        }

        protected override void OnReceive(ServerSessionContainer serverSession)
        {
            var serverChanges = serverSession.Require<VoxelChangesProperty>();
            UIntProperty serverTick = serverSession.Require<ServerStampComponent>().tick;

            if (serverChanges.Count > 0)
            {
                VoxelChangesProperty changes = m_ChangesPool.Obtain();
                changes.SetTo(serverChanges);
                m_Changes.Add(serverTick, changes);
            }

            var tickSkipped = false;
            if (m_Pointer.WithoutValue || serverTick - m_Pointer == 1 || (tickSkipped = serverTick - m_Pointer > serverSession.Require<TickRateProperty>() * 3))
            {
                ApplyStoredChanges();
                if (m_Pointer.TryWithValue(out uint pointer) && tickSkipped) Debug.LogError($"Did not receive voxel changes for {pointer}");
                m_Pointer.Value = serverTick;
            }
        }

        private void ApplyStoredChanges()
        {
            foreach (VoxelChangesProperty changes in m_Changes.Values)
            foreach (KeyValuePair<Position3Int, VoxelChange> pair in changes.Map)
            {
                ChunkManager.Singleton.EvaluateVoxelChange(pair.Key, pair.Value, null, false, false, m_Transaction);
            }
            m_Transaction.Commit();
            m_Changes.Clear();
            m_ChangesPool.ReturnAll();
        }
    }
}