using Swihoni.Sessions.Config;
using UnityEditor;

namespace Swihoni.Sessions.Editor
{
    public static class MenuItems
    {
        [MenuItem("Voxelfield/Save Default Config")]
        public static void SaveDefault() => ConfigManagerBase.WriteDefaults();
    }
}