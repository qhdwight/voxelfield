using System.Net;
using Swihoni.Components;
using Swihoni.Sessions;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IPEndPoint ipEndPoint)
            : base(CompoundComponents.SessionElements, ipEndPoint)
        {
        }

        protected override void SettingsTick(Container serverSession)
        {
            MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);
        }

        public override bool IsPaused => ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}