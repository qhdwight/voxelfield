using Swihoni.Sessions;
using Swihoni.Sessions.Config;

namespace Voxelfield
{
    public static class Extensions
    {
        public static ConfigManager GetConfig() => (ConfigManager) ConfigManagerBase.Active;
    }
}