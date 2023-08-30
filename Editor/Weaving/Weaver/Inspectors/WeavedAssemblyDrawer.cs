using UnityEditor;
using UnityEngine;

namespace Elympics.Weaver.Editors
{
    [CustomPropertyDrawer(typeof(WeavedAssembly))]
    public class WeavedAssemblyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            {
                const float buttonWidth = 20;
                var relativePath = property.FindPropertyRelative("m_RelativePath");
                var isActive = property.FindPropertyRelative("m_IsActive");
                var shouldThrowIfNotFound = property.FindPropertyRelative("shouldThrowIfNotFound");
                position.width -= buttonWidth * 2;
                EditorGUI.LabelField(position, relativePath.stringValue, EditorStyles.textArea);
                position.x += position.width;
                position.width = buttonWidth;
                isActive.boolValue = EditorGUI.Toggle(position, isActive.boolValue);
                position.x += position.width;
                position.width = buttonWidth;
                shouldThrowIfNotFound.boolValue = EditorGUI.Toggle(position, shouldThrowIfNotFound.boolValue);
            }
            if (EditorGUI.EndChangeCheck())
                _ = property.serializedObject.ApplyModifiedProperties();
        }
    }
}
