using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util.Math;
using Voxel;

namespace Voxelfield.Session
{
    public class ClientInjector : VoxelInjector
    {
        private readonly Pool<VoxelChangeTransaction> m_Transactions = new Pool<VoxelChangeTransaction>(1, () => new VoxelChangeTransaction());
        private readonly UIntProperty m_Pointer = new UIntProperty();

        protected internal override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true) { }

        protected internal override void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, bool destroyBlocks = false,
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
                VoxelChangeTransaction transaction = m_Transactions.Obtain();
                foreach ((Position3Int position, VoxelChangeData change) in changed)
                    transaction.AddChange(position, change);
            }

            if (m_Pointer.WithoutValue || serverTick - m_Pointer == 1)
            {
                Flush();
                m_Pointer.Value = serverTick;
            }
        }

        private void Flush()
        {
            foreach (VoxelChangeTransaction transaction in m_Transactions.InUse)
                transaction.Commit();
            m_Transactions.ReturnAll();
        }
    }
}