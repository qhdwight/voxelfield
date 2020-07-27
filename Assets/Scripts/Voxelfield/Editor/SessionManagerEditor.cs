using UnityEditor;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Editor
{
    [CustomEditor(typeof(SessionManager))]
    public class SessionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            bool forceUpdate = GUILayout.Button("Save Custom Map");
            if (forceUpdate) SessionManager.SaveCustomMap();
        }
    }
}