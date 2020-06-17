using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Util.Math;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class ClientInjector : VoxelInjector
    {
        private readonly VoxelChangeTransaction m_Transaction = new VoxelChangeTransaction();

        protected override void OnSettingsTick(Container session) => MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);

        protected internal override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true) { }

        protected internal override void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
        {
        }

        protected override void OnReceive(ServerSessionContainer serverSession)
        {
            var changed = serverSession.Require<ChangedVoxelsProperty>();
            foreach ((Position3Int position, VoxelChangeData change) in changed)
                m_Transaction.AddChange(position, change);
            m_Transaction.Commit();
        }

        protected override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}