using System.Net;
using Swihoni.Components;
using Swihoni.Sessions;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Server : ServerBase
    {
        public Server(IPEndPoint ipEndPoint)
            : base(CompoundComponents.SessionElements, ipEndPoint)
        {
        }

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);
        }
        
        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}