#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomEditor(typeof(ElympicsBehavioursManager))]
    [CanEditMultipleObjects]
    public class ElympicsBehavioursManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var elympicsBehavioursManager = (ElympicsBehavioursManager)target;
            if (GUILayout.Button("Refresh Elympics Behaviours", GUILayout.Width(200)))
                elympicsBehavioursManager.RefreshElympicsBehavioursView();
            base.OnInspectorGUI();
        }
    }
}
#endif
