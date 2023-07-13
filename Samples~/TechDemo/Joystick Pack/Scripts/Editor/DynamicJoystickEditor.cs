using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DynamicJoystick))]
public class DynamicJoystickEditor : JoystickEditor
{
    private SerializedProperty moveThreshold;

    protected override void OnEnable()
    {
        base.OnEnable();
        moveThreshold = serializedObject.FindProperty("moveThreshold");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (background != null)
        {
            var backgroundRect = (RectTransform)background.objectReferenceValue;
            backgroundRect.anchorMax = Vector2.zero;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.pivot = center;
        }
    }

    protected override void DrawValues()
    {
        base.DrawValues();
        _ = EditorGUILayout.PropertyField(moveThreshold, new GUIContent("Move Threshold", "The distance away from the center input has to be before the joystick begins to move."));
    }
}
