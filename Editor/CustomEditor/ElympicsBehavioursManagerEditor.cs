using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomEditor(typeof(ElympicsBehavioursManager))]
    [CanEditMultipleObjects]
    public class ElympicsBehavioursManagerEditor : UnityEditor.Editor
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
