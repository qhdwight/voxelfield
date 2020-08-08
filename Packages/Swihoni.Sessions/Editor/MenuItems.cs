using Swihoni.Sessions.Config;
using UnityEditor;

namespace Swihoni.Sessions.Editor
{
    public static class MenuItems
    {
        [MenuItem("Session/Save Default Config")]
        public static void SaveDefaultConfig() => ConfigManagerBase.WriteDefaults(true);
    }
}