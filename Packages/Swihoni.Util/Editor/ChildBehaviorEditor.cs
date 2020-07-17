using Swihoni.Util.Animation;
using UnityEditor;
using UnityEngine;

namespace Swihoni.Util.Editor
{
    [CustomEditor(typeof(ChildBehavior))]
    public class ChildBehaviorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            bool forceUpdate = GUILayout.Button("Force Update");
            if (forceUpdate) (target as ChildBehavior)?.Evaluate();
        }

        private void OnSceneGUI()
        {
            Handles.BeginGUI();

            Handles.EndGUI();
        }
    }
}