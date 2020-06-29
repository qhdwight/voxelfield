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
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.SetVoxelData(worldPosition, change, chunk, updateMesh);
            changed.SetVoxel(worldPosition, change);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
        {
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            base.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changed);
            m_MasterChanges.AddAllFrom(changed);
        }

        protected internal override void VoxelTransaction(VoxelChangeTransaction uncommitted)
        {
            var changed = Manager.GetLatestSession().Require<ChangedVoxelsProperty>();
            foreach ((Position3Int position, VoxelChangeData change) in uncommitted)
                changed.SetVoxel(position, change);
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
            base.OnSettingsTick(serverSession);
            MapManager manager = MapManager.Singleton;
            var mapName = serverSession.Require<VoxelMapNameProperty>();
            mapName.SetTo(DebugBehavior.Singleton.MapNameProperty);
            manager.SetMap(mapName);
            if (manager.Map != null) manager.Map.changedVoxels = m_MasterChanges;
        }
    }
}