using UnityEditor;

namespace Swihoni.Components.Editor
{
    [CustomPropertyDrawer(typeof(StringProperty), true)]
    public class StringPropertyDrawer : PropertyDrawer
    {
        // public override VisualElement CreatePropertyGUI(SerializedProperty property)
        // {
        //     var container = new VisualElement();
        //     container.Add(new PropertyField(property.FindPropertyRelative("m_Flags")));
        //     return container;
        // }

        // public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        // {
        //     string text = EditorGUILayout.TextField("Test");
        // }
    }
}