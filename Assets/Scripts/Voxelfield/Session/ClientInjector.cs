using System.Collections.Generic;
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

        protected internal override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true) { }

        protected internal override void SetVoxelRadius(in Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false,
                                                        bool destroyBlocks = false, bool isAdditive = false, in VoxelChangeData additiveChange = default,
                                                        ChangedVoxelsProperty changedVoxels = null)
        {
        }

        protected internal override void VoxelTransaction(VoxelChangeTransaction uncommitted) { }

        protected override void OnReceive(ServerSessionContainer serverSession)
        {
            var changed = serverSession.Require<ChangedVoxelsProperty>();

            UIntProperty serverTick = serverSession.Require<ServerStampComponent>().tick;

            if (changed.Count > 0)
            {
                VoxelChangeTransaction transaction = m_TransactionPool.Obtain();
                foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in changed.Map)
                    transaction.AddChange(pair.Key, pair.Value);
                m_Transactions.Add(serverTick, transaction);
            }

            var tickSkipped = false;
            if (m_Pointer.WithoutValue || serverTick - m_Pointer == 1 || (tickSkipped = serverTick - m_Pointer > serverSession.Require<TickRateProperty>() * 3))
            {
                ApplyStoredChanges();
                if (m_Pointer.WithValue && tickSkipped) Debug.LogError($"Did not receive voxel changes for {m_Pointer}");
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