using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Elympics.Editor.Weaving.Settings
{
    public class WeaverSettings : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("m_IsEnabled")]
        private bool isEnabled = true;

        [SerializeField, FormerlySerializedAs("m_RequiredScriptingSymbols.value")]
        [Tooltip("Required DEFINE directives for this settings to be included. Separated with semicolons")]
        private string requiredScriptingSymbols = "";

        [SerializeField, FormerlySerializedAs("m_WeavedAssemblies")]
        private List<WeavedAssembly> weavedAssemblies = new();

        public bool IsEnabled => isEnabled;
        public string RequireScriptingSymbols => requiredScriptingSymbols;
        public List<WeavedAssembly> WeavedAssemblies => weavedAssemblies;

        private void OnEnable() => Debug.Log($"[WeaverSettings] [{AssetDatabase.GetAssetPath(this)}] OnEnable");
    }
}

