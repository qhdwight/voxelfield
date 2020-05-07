using Swihoni.Components;
using Swihoni.Sessions;
using Voxel.Map;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host() : base(CompoundComponents.SessionElements) { }

        protected override void SettingsTick(Container serverSession)
        {
            base.SettingsTick(serverSession);

            string mapName = DebugBehavior.Singleton.mapName;
            serverSession.Require<VoxelMapNameProperty>().SetString(builder => builder.Append(mapName));
            MapManager.Singleton.SetMap(mapName);
        }

        public override void Dispose()
        {
            base.Dispose();
            MapManager.Singleton.SetMap("Menu");
        }
    }
}