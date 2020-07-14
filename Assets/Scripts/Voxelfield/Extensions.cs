using Swihoni.Sessions;

namespace Voxelfield
{
    public static class Extensions
    {
        public static ConfigManager GetConfig() => (ConfigManager) ConfigManagerBase.Singleton;
    }
}