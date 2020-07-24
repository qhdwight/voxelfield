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
        private readonly Pool<VoxelChangeTransaction> m_TransactionPool = new Pool<VoxelChangeTransaction>(1, () => new VoxelChangeTransaction());
        private readonly UIntProperty m_Pointer = new UIntProperty();
        private readonly SortedDictionary<uint, VoxelChangeTransaction> m_Transactions = new SortedDictionary<uint, VoxelChangeTransaction>();

        protected internal override void ApplyVoxelChange(in Position3Int worldPosition, in VoxelChange change, Chunk chunk = null, bool updateMesh = true) { }

        protected internal override void VoxelTransaction(VoxelChangeTransaction uncommitted) { }

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
            var changed = serverSession.Require<ChangedVoxelsProperty>();

            UIntProperty serverTick = serverSession.Require<ServerStampComponent>().tick;

            if (changed.Count > 0)
            {
                VoxelChangeTransaction transaction = m_TransactionPool.Obtain();
                foreach (KeyValuePair<Position3Int, VoxelChange> pair in changed.Map)
                    transaction.AddChange(pair.Key, pair.Value);
                m_Transactions.Add(serverTick, transaction);
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
            foreach (VoxelChangeTransaction transaction in m_Transactions.Values)
                transaction.Commit();
            m_Transactions.Clear();
            m_TransactionPool.ReturnAll();
        }
    }
}