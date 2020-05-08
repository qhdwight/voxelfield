using Swihoni.Components;
using Swihoni.Sessions;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class Server : ServerBase
    {
        public Server()
            : base(CompoundComponents.SessionElements)
        {
        }

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            MapManager.Singleton.SetMap(DebugBehavior.Singleton.mapName);
            IsPaused = ChunkManager.Singleton.ProgressInfo.stage == MapLoadingStage.Completed;
        }
    }
}