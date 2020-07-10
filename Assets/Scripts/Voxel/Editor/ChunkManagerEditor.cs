using UnityEditor;
using UnityEngine;

namespace Voxel.Editor
{
    [CustomEditor(typeof(ChunkManager))]
    public class ChunkManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            bool forceUpdate = GUILayout.Button("Force Update Chunks");
            if (forceUpdate && target is ChunkManager manager)
                foreach (Chunk chunk in manager.Chunks.Values)
                    chunk.UpdateAndApply();
        }
    }
}