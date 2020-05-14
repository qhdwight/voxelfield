using System.Net;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Server : ServerBase, IMiniProvider
    {
        public class Mini : MiniBase
        {
            public Mini(SessionBase session) : base(session) { }

            public override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            {
                base.SetVoxelData(worldPosition, change, chunk, updateMesh);
                var changed = m_Session.GetLatestSession().Require<ChangedVoxelsProperty>();
                changed.SetVoxel(worldPosition, change);
            }

            public override void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, in VoxelChangeMap changedVoxels = null)
            {
                base.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, in changedVoxels);
            }
        }

        private readonly Mini m_Mini;

        public Server(IPEndPoint ipEndPoint) : base(CompoundComponents.SessionElements, ipEndPoint) => m_Mini = new Mini(this);

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);
        }

        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;

        public void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData changeData, Chunk chunk = null, bool updateMesh = true) { }

        public MiniBase GetMini() => m_Mini;
    }
}