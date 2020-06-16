using System.Net;
using LiteNetLib;
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
            private readonly ChangedVoxelsProperty m_MasterChanges = new ChangedVoxelsProperty();

            public Mini(SessionBase session) : base(session) => m_MasterChanges.IsMaster = true;

            public override void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            {
                var changed = m_Session.GetLatestSession().Require<ChangedVoxelsProperty>();
                base.SetVoxelData(worldPosition, change, chunk, updateMesh);
                changed.SetVoxel(worldPosition, change);
                m_MasterChanges.AddAllFrom(changed);
            }

            public override void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
            {
                var changed = m_Session.GetLatestSession().Require<ChangedVoxelsProperty>();
                base.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changed);
                m_MasterChanges.AddAllFrom(changed);
            }

            public void MergeInitialData(NetPeer peer, Container serverSession, Container sendSession)
            {
                var changedVoxels = sendSession.Require<ChangedVoxelsProperty>();
                changedVoxels.SetFromIfWith(m_MasterChanges);
            }
        }

        private readonly Mini m_Mini;

        public Server(IPEndPoint ipEndPoint) : base(CompoundComponents.SessionElements, ipEndPoint, MiniBase.AcceptConnection) => m_Mini = new Mini(this);

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);
            MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);
        }

        protected override void MergeInitialData(NetPeer peer, Container serverSession, Container sendSession) => m_Mini.MergeInitialData(peer, serverSession, sendSession);

        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;

        public MiniBase GetMini() => m_Mini;
    }
}