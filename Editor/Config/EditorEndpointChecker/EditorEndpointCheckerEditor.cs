using UnityEditor;
using UnityEngine;

namespace Elympics
{
	public static class EditorEndpointCheckerEditor
	{
		public static void DrawEndpointField(SerializedProperty endpoint, string label)
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(endpoint, new GUIContent(label), GUILayout.MaxWidth(float.MaxValue));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Separator();
		}
	}
}