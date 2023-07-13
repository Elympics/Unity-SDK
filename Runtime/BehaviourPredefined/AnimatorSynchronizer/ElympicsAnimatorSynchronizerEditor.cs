#if UNITY_EDITOR
using System.Linq;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Elympics
{
    [CustomEditor(typeof(ElympicsAnimatorSynchronizer))]
    internal partial class ElympicsAnimatorSynchronizerEditor : Editor
    {
        private ElympicsAnimatorSynchronizer _animatorSynchronizer;
        private AnimatorController _controller;
        private Animator _animator;

        private NamedSyncStatusArrayWrapper _disabledParameters;
        private NamedSyncStatusArrayWrapper _disabledLayers;

        private bool _showLayerWeights = true;
        private bool _showParameters = true;

        private GUIStyle _summaryLabelStyle;

        private void OnEnable()
        {
            _animatorSynchronizer = (ElympicsAnimatorSynchronizer)target;
            _animator = _animatorSynchronizer.GetComponent<Animator>();

            if (_animator)
                _controller = GetEffectiveController(_animator) as AnimatorController;

            _disabledParameters = new NamedSyncStatusArrayWrapper(serializedObject.FindProperty("disabledParameters"));
            _disabledLayers = new NamedSyncStatusArrayWrapper(serializedObject.FindProperty("disabledLayers"));
            UpdateLayersAndParameters();
        }

        public override void OnInspectorGUI()
        {
            _summaryLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
            base.OnInspectorGUI();

            var isPlayingOrClone = EditorApplication.isPlaying || ElympicsClonesManager.IsClone();

            if (_animator == null)
            {
                EditorGUILayout.HelpBox(Label_GameObjectNullWarning, MessageType.Warning);
                return;
            }

            if (_controller == null)
            {
                EditorGUILayout.HelpBox(Label_AnimatorControllerMissingWarning, MessageType.Warning);
                return;
            }

            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox(Label_PlayModeModificationWarning, MessageType.Warning);
            if (ElympicsClonesManager.IsClone())
                EditorGUILayout.HelpBox(Label_CloneModificationWarning, MessageType.Warning);

            EditorGUI.BeginDisabledGroup(isPlayingOrClone);

            if (GUILayout.Button(Label_RefreshButton))
                UpdateLayersAndParameters();

            EditorGUI.BeginChangeCheck();
            DrawLayers();
            EditorGUILayout.Space();
            DrawParameters();
            if (EditorGUI.EndChangeCheck())
                _ = serializedObject.ApplyModifiedProperties();

            EditorGUI.EndDisabledGroup();
        }

        private void UpdateLayersAndParameters()
        {
            _disabledLayers.UpdateList(_controller.layers.Select(x => x.name));
            _disabledParameters.UpdateList(_controller.parameters.Select(x => x.name));
            _ = serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayers()
        {
            _showLayerWeights = EditorGUILayout.Foldout(_showLayerWeights, Label_Layers);
            EditorGUILayout.LabelField(Label_LayersTooltip, _summaryLabelStyle);

            if (_controller.layers.Length == 0)
            {
                EditorGUILayout.HelpBox(Label_NoAnimatorLayersWarning, MessageType.Warning);
                return;
            }

            if (_showLayerWeights)
                DrawNamedSyncStatusArray(_disabledLayers);
        }

        private void DrawParameters()
        {
            _showParameters = EditorGUILayout.Foldout(_showParameters, Label_Parameters);
            EditorGUILayout.LabelField(Label_ParametersTooltip, _summaryLabelStyle);

            if (_controller.parameters.Length == 0)
            {
                EditorGUILayout.HelpBox(Label_NoAnimatorParametersWarning, MessageType.Warning);
                return;
            }

            if (_showParameters)
                DrawNamedSyncStatusArray(_disabledParameters);
        }

        private static void DrawNamedSyncStatusArray(NamedSyncStatusArrayWrapper arrayWrapper)
        {
            foreach (var syncStatus in arrayWrapper.Elements)
            {
                _ = EditorGUILayout.BeginVertical();
                _ = EditorGUILayout.BeginHorizontal();
                var enabled = EditorGUILayout.Toggle(new GUIContent(syncStatus.Name), syncStatus.Enabled);
                if (enabled != syncStatus.Enabled)
                    arrayWrapper.SetEnabled(syncStatus.Name, enabled);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private static RuntimeAnimatorController GetEffectiveController(Animator animator)
        {
            var controller = animator.runtimeAnimatorController;
            var overrideController = controller as AnimatorOverrideController;
            while (overrideController != null)
            {
                controller = overrideController.runtimeAnimatorController;
                overrideController = controller as AnimatorOverrideController;
            }

            return controller;
        }
    }
}
#endif
