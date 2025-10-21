using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomPropertyDrawer(typeof(ElympicsBool))]
    [CustomPropertyDrawer(typeof(ElympicsFloat))]
    [CustomPropertyDrawer(typeof(ElympicsInt))]
    [CustomPropertyDrawer(typeof(ElympicsQuaternion))]
    [CustomPropertyDrawer(typeof(ElympicsString))]
    [CustomPropertyDrawer(typeof(ElympicsVector2))]
    [CustomPropertyDrawer(typeof(ElympicsVector3))]
    public class ElympicsVarPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("currentValue");
            _ = EditorGUI.PropertyField(position, valueProperty, label);
        }
    }
}
