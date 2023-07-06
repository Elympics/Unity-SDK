using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [InitializeOnLoad]
    public class ElympicsBehaviourHierarchyIcon
    {
        private const float IconWidth = 15;

        private static readonly Texture2D IconForRegular;
        private static readonly Texture2D IconForManager;
        private static readonly Texture2D IconForInput;

        static ElympicsBehaviourHierarchyIcon()
        {
            IconForRegular = Resources.Load<Texture2D>("Gizmos/elympics_icon_green");
            IconForManager = Resources.Load<Texture2D>("Gizmos/elympics_icon_purple");
            IconForInput = Resources.Load<Texture2D>("Gizmos/elympics_icon_cyan");

            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
                return;

            if (gameObject.TryGetComponent<ElympicsBehavioursManager>(out _))
                DrawIcon(rect, IconForManager);
            else if (gameObject.TryGetComponent<IInputHandler>(out _))
                DrawIcon(rect, IconForInput);
            else if (gameObject.TryGetComponent<ElympicsBehaviour>(out _))
                DrawIcon(rect, IconForRegular);
        }

        private static void DrawIcon(Rect rect, Texture2D icon)
        {
            if (icon == null)
                return;
            EditorGUIUtility.SetIconSize(new Vector2(IconWidth, IconWidth));
            var padding = new Vector2(5, 0);
            var iconDrawRect = new Rect(
                                   rect.xMax - (IconWidth + padding.x),
                                   rect.yMin,
                                   rect.width,
                                   rect.height);
            var iconGUIContent = new GUIContent(icon);
            EditorGUI.LabelField(iconDrawRect, iconGUIContent);
            EditorGUIUtility.SetIconSize(Vector2.zero);
        }
    }
}
