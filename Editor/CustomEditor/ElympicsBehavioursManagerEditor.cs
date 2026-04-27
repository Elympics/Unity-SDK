#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomEditor(typeof(ElympicsBehavioursManager))]
    [CanEditMultipleObjects]
    internal class ElympicsBehavioursManagerEditor : UnityEditor.Editor
    {
        private bool _initialized;
        private List<ElympicsBehaviour> _elympicsBehaviours = new();
        private Vector2 _elympicsBehavioursViewScrollPos;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var buttonLabel = _initialized ? "Refresh ElympicsBehaviours" : "List ElympicsBehaviours";
            if (GUILayout.Button(buttonLabel))
                RefreshElympicsBehavioursView();
            if (_initialized)
                DrawBehavioursView();
        }

        private void RefreshElympicsBehavioursView()
        {
            var behavioursManager = (ElympicsBehavioursManager)serializedObject.targetObject;
            _elympicsBehaviours = behavioursManager.FindElympicsBehavioursSorted();
            _initialized = true;
        }

        private void DrawBehavioursView()
        {
            if (_elympicsBehaviours.Count == 0)
            {
                EditorGUILayout.LabelField("No ElympicsBehaviours found.");
                return;
            }

            var height = GUILayout.Height(Mathf.Min(200f, _elympicsBehaviours.Count * 20f) + 2f);
            _elympicsBehavioursViewScrollPos = EditorGUILayout.BeginScrollView(_elympicsBehavioursViewScrollPos, height);
            foreach (var behaviour in _elympicsBehaviours)
            {
                _ = EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                _ = EditorGUILayout.IntField(behaviour.NetworkId, GUILayout.Width(40));
                _ = EditorGUILayout.ObjectField(behaviour, typeof(ElympicsBehaviour), true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
