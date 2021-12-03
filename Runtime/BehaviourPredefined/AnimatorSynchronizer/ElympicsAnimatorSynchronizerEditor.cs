#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsAnimatorSynchronizer))]
	internal partial class ElympicsAnimatorSynchronizerEditor : Editor
	{
		private ElympicsAnimatorSynchronizer _animatorSynchronizer;
		private AnimatorController           _controller;
		private Animator                     _animator;

		private GUIStyle summaryLabelStyle;

		private void OnEnable()
		{
			_animatorSynchronizer = (ElympicsAnimatorSynchronizer)this.target;
			_animator = _animatorSynchronizer.GetComponent<Animator>();

			if (_animator)
				_controller = this.GetEffectiveController(_animator) as AnimatorController;
		}

		public override void OnInspectorGUI()
		{
			summaryLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
			base.OnInspectorGUI();

			if (_animator == null)
				EditorGUILayout.HelpBox(Label_GameObjectNullWarning, MessageType.Warning);

			if (GetLayerCount() == 0)
				EditorGUILayout.HelpBox(Label_NoAnimatorLayersWarning, MessageType.Warning);

			if (GetParameterCount() == 0)
				EditorGUILayout.HelpBox(Label_NoAnimatorParametersWarning, MessageType.Warning);

			_animatorSynchronizer.PrepareStatusesToUpdate();
			DrawLayers();
			EditorGUILayout.Space();
			DrawParameters();
			_animatorSynchronizer.RemoveOutdatedStatuses();

			serializedObject.ApplyModifiedProperties();
		}


		private void DrawLayers()
		{
			SerializedProperty showLayersWeight = this.serializedObject.FindProperty(ShowLayersWeightPropertyName);
			showLayersWeight.boolValue = EditorGUILayout.Foldout(showLayersWeight.boolValue, Label_Layers);
			EditorGUILayout.LabelField(Label_LayersTooltip, summaryLabelStyle);

			if (showLayersWeight.boolValue)
				for (int i = 0; i < _controller.layers.Length; i++)
					DrawLayer(_controller.layers[i], i);
		}

		private void DrawLayer(AnimatorControllerLayer layer, int index)
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			var changedValue = EditorGUILayout.Toggle(new GUIContent(layer.name), _animatorSynchronizer.GetLayer(index));
			_animatorSynchronizer.SetLayer(index, changedValue);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private void DrawParameters()
		{
			SerializedProperty showParameters = this.serializedObject.FindProperty(ShowParametersPropertyName);
			showParameters.boolValue = EditorGUILayout.Foldout(showParameters.boolValue, Label_Parameters);
			EditorGUILayout.LabelField(Label_ParametersTooltip, summaryLabelStyle);

			if (showParameters.boolValue)
				for (int i = 0; i < _controller.parameters.Length; i++)
					DrawParameter(_controller.parameters[i], i);
		}

		private void DrawParameter(AnimatorControllerParameter parameter, int index)
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			var parameterMode = _animatorSynchronizer.GetParameter(_controller.parameters[index].type, Animator.StringToHash(parameter.name));
			var changedValue = EditorGUILayout.Toggle(new GUIContent(parameter.name), parameterMode);
			_animatorSynchronizer.SetParameter(_controller.parameters[index].type, Animator.StringToHash(parameter.name), changedValue);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}


		private RuntimeAnimatorController GetEffectiveController(Animator animator)
		{
			RuntimeAnimatorController controller = animator.runtimeAnimatorController;
			AnimatorOverrideController overrideController = controller as AnimatorOverrideController;
			while (overrideController != null)
			{
				controller = overrideController.runtimeAnimatorController;
				overrideController = controller as AnimatorOverrideController;
			}

			return controller;
		}

		private int GetLayerCount()     => _controller == null ? 0 : _controller.layers.Length;
		private int GetParameterCount() => _controller == null ? 0 : _controller.parameters.Length;
	}
}
#endif