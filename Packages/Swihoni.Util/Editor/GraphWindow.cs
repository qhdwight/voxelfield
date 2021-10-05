using UnityEditor;
using UnityEngine;

namespace Swihoni.Util.Editor
{
    public class GraphWindow : EditorWindow
    {
        private AnimationCurve m_Curve = new();

        [MenuItem("Window/Graph")]
        private static void ShowWindow() => GetWindow<GraphWindow>("Graph");

        private void OnEnable()
        {
            for (var i = 0; i < EditorGraph.Keys.Length; i++)
                EditorGraph.Keys[i].time = i / (float) EditorGraph.Size;
        }

        private void OnGUI()
        {
            m_Curve = EditorGUILayout.CurveField(m_Curve, Color.white, new Rect(0.0f, EditorGraph.minValue, 1.0f, EditorGraph.maxValue - EditorGraph.minValue),
                                                 GUILayout.Height(150.0f));
        }

        private void Update()
        {
            m_Curve.keys = EditorGraph.Keys;
            Repaint();
        }
    }
}