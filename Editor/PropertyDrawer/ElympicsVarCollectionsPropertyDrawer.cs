using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomPropertyDrawer(typeof(ElympicsList<>))]
    [CustomPropertyDrawer(typeof(ElympicsArray<>))]
    public class ElympicsVarCollectionsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("values");
            var enabledProperty = property.FindPropertyRelative("_enabledSynchronization");

            const float toggleWidth = 60f; // space for checkbox + 'Sync' label
            const float spacing = 4f;
            const float checkboxSize = 16f;

            var fieldRect = new Rect(position.x, position.y, position.width - toggleWidth - spacing, position.height);
            var toggleRect = new Rect(position.x + position.width - toggleWidth, position.y, toggleWidth, position.height);

            // Value field (with label)
            _ = EditorGUI.PropertyField(fieldRect, valueProperty, label, true);

            _ = EditorGUI.BeginProperty(position, label, property);

            // Draw label and checkbox such that checkbox is on the right side of the label
            var labelRect = new Rect(toggleRect.x, toggleRect.y, toggleRect.width - checkboxSize - 4f, toggleRect.height);
            var checkboxRect = new Rect(toggleRect.x + toggleRect.width - checkboxSize, toggleRect.y + (toggleRect.height - checkboxSize) / 2f, checkboxSize, checkboxSize);

            EditorGUI.LabelField(labelRect, new GUIContent("Sync", "Enable or disable synchronization of this variable."));
            enabledProperty.boolValue = EditorGUI.Toggle(checkboxRect, enabledProperty.boolValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("values");
            return EditorGUI.GetPropertyHeight(valueProperty, label, true);
        }
    }
}
