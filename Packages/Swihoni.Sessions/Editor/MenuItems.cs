using Swihoni.Sessions.Config;
using UnityEditor;

namespace Swihoni.Sessions.Editor
{
    public static class MenuItems
    {
        [MenuItem("Config/Save Default")]
        public static void SaveDefault() => ConfigManagerBase.WriteDefaults();
    }
}