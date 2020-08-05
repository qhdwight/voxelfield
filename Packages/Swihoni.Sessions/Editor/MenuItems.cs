using Swihoni.Sessions.Config;
using UnityEditor;
using UnityEngine;

namespace Swihoni.Sessions.Editor
{
    public static class MenuItems
    {
        [MenuItem("Session/Save Default Config")]
        public static void SaveDefaultConfig() => ConfigManagerBase.WriteDefaults(true);
    }
}