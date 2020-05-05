using UnityEditor;
using UnityEngine;

namespace Swihoni.Util.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerMaskDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, label,  property.intValue);
        }
    }
}