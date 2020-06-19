using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;
using Voxel.Map;

namespace Compound.Session
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

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<ChangedVoxelsProperty>();
            changedVoxels.SetTo(m_MasterChanges);
        }

        protected override void OnSettingsTick(Container serverSession) => MapManager.Singleton.SetMap(DebugBehavior.Singleton.MapName);

        protected override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}