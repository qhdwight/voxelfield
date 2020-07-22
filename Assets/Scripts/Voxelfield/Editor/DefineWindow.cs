using UnityEditor;
using UnityEngine;

namespace Voxelfield.Editor
{
    public class DefineWindow : EditorWindow
    {
        [MenuItem("Voxelfield/Define Manager")]
        private static void ShowWindow()
        {
            var window = GetWindow<DefineWindow>();
            window.titleContent = new GUIContent("Define Manager");

            window.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Release Client"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "VOXELFIELD_RELEASE_CLIENT");
            }
            if (GUILayout.Button("Release Server"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "VOXELFIELD_RELEASE_SERVER");
            }
            if (GUILayout.Button("Reset"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Empty);
            }
        }
    }
}