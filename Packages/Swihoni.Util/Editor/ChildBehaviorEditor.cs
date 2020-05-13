using Swihoni.Util.Animation;
using UnityEditor;

namespace Swihoni.Util.Editor
{
    [CustomEditor(typeof(ChildBehavior))]
    public class ChildBehaviorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() { base.OnInspectorGUI(); }
    }
}