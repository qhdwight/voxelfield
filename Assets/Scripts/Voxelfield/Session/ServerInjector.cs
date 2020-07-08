using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;
using Voxel.Map;

namespace Voxelfield.Session
{
    public class ServerInjector : VoxelInjector
    {
        private readonly ChangedVoxelsProperty m_MasterChanges = new ChangedVoxelsProperty();

        protected internal override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
        {
            if (MapManager.Singleton.Models.ContainsKey(worldPosition)) return;
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.SetVoxelData(worldPosition, change, chunk, updateMesh);
            changed.Set(worldPosition, change);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void SetVoxelRadius(in Position3Int worldPosition, float radius,
                                                        bool replaceGrassWithDirt = false, bool destroyBlocks = false, bool additive = false, ChangedVoxelsProperty changedVoxels = null)
        {
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.SetVoxelRadius(worldPosition, radius, replaceGrassWithDirt, destroyBlocks, additive, changed);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void VoxelTransaction(VoxelChangeTransaction uncommitted)
        {
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            foreach ((Position3Int position, VoxelChangeData change) in uncommitted)
                changed.Set(position, change);
            m_MasterChanges.AddAllFrom(changed);
            base.VoxelTransaction(uncommitted); // Commit
        }

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<ChangedVoxelsProperty>();
            changedVoxels.SetTo(m_MasterChanges);
        }

        protected override void OnSettingsTick(Container serverSession)
        {
            serverSession.Require<VoxelMapNameProperty>().SetTo(DebugBehavior.Singleton.MapNameProperty);
            base.OnSettingsTick(serverSession); // Set map
        }
    }
}