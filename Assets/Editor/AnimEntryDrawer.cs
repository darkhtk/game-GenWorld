using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimationDef.AnimEntry))]
public class AnimEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var clipProp = property.FindPropertyRelative("clip");
        bool missing = clipProp != null && clipProp.objectReferenceValue == null;

        if (missing)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.backgroundColor = prev;
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
