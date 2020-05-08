using System.Net;
using Swihoni.Components;
using Swihoni.Sessions;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host() : base(CompoundComponents.SessionElements, new IPEndPoint(IPAddress.Loopback, 7777)) { }

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            string mapName = DebugBehavior.Singleton.mapName;
            serverSession.Require<VoxelMapNameProperty>().SetString(builder => builder.Append(mapName));
            MapManager.Singleton.SetMap(mapName);
        }
        
        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}