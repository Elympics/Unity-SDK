#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsTransformSynchronizer))]
	public class ElympicsTransformSynchronizerEditor : Editor
	{
		private GUIStyle _warningStyle;
		private ElympicsTransformSynchronizer _target;
		private string Label_RigidBodyAsReduntandSynchronizerWarning = "<color=yellow>Warning! Behaviour contains Transform Synchronizer. Additional RigidBody synchronizer may cause performance and synchronization issues.</color> <a href=\"https://docs.elympics.cc/guide/state/#elympicsrigidbodysynchronizer\"></a>";

		/* todo add LINK text in href RichText after after migrating to 2021.3 where Unity GUI is able to detect hyperlink text click ~kpieta 25.10.2022 https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorGUI-hyperLinkClicked.html.*/

		public override void OnInspectorGUI()
		{
			_warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Normal, wordWrap = true, richText = true };
			if (_target == null)
			{
				_target = serializedObject.targetObject as ElympicsTransformSynchronizer;
			}
			if (_target.TryGetComponent<ElympicsRigidBodySynchronizer>(out _) || _target.TryGetComponent<ElympicsRigidBody2DSynchronizer>(out _))
			{
				EditorGUILayout.Space(5);
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField(Label_RigidBodyAsReduntandSynchronizerWarning, _warningStyle);

				if (GUILayout.Button(ElympicsDocumentationUrls.Label_Documentation, GUILayout.ExpandWidth(false)))
					Application.OpenURL(ElympicsDocumentationUrls.Link_RigidBodySynchronizerDocumentation);

				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
			}
			base.OnInspectorGUI();
		}

	}
}

#endif
