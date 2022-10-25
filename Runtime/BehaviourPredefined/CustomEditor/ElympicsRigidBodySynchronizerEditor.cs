#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsRigidBodySynchronizer))]
	public class ElympicsRigidBodySynchronizerEditor : Editor
	{
		private GUIStyle _warningStyle;
		private ElympicsRigidBodySynchronizer _target;
		private string Label_TransformAsReduntandSynchronizerWarning = "<color=yellow>Warning! Behaviour contains RigidBody Synchronizer. Additional Transform synchronizer may cause performance and synchronization issues.</color> <a href=\"https://docs.elympics.cc/guide/state/#elympicsrigidbodysynchronizer\">Link</a>";

		/* todo add LINK text in href RichText after after migrating to 2021.3 where Unity GUI is able to detect hyperlink text click ~kpieta 25.10.2022 https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorGUI-hyperLinkClicked.html.*/

		public override void OnInspectorGUI()
		{
			_warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Normal, wordWrap = true, richText = true };
			if (_target == null)
			{
				_target = serializedObject.targetObject as ElympicsRigidBodySynchronizer;
			}
			if (_target.TryGetComponent<ElympicsTransformSynchronizer>(out _))
			{
				EditorGUILayout.Space(5);
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField(Label_TransformAsReduntandSynchronizerWarning, _warningStyle);

				if (GUILayout.Button(ElympicsDocumentationUrls.Label_Documentation, GUILayout.ExpandWidth(false)))
					Application.OpenURL(ElympicsDocumentationUrls.Link_TransfromSynchronizerDocumentation);

				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
			}
			base.OnInspectorGUI();
		}
	}
}

#endif
