using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Host : HostBase, IMiniProvider
    {
        private class Mini : Server.Mini
        {
            public Mini(SessionBase session) : base(session) { }
        }

        private readonly Mini m_Mini;

        public Host() : base(CompoundComponents.SessionElements, new IPEndPoint(IPAddress.Loopback, 7777), MiniBase.AcceptConnection) => m_Mini = new Mini(this);

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            string mapName = DebugBehavior.Singleton.mapName;
            serverSession.Require<VoxelMapNameElement>().SetString(builder => builder.Append(mapName));
            MapManager.Singleton.SetMap(mapName);
        }

        protected override void MergeInitialData(NetPeer peer, Container serverSession, Container sendSession) => m_Mini.MergeInitialData(peer, sendSession, sendSession);

        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;

        public void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData changeData, Chunk chunk = null, bool updateMesh = true) { }

        public MiniBase GetMini() => m_Mini;
    }
}