using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace Elympics.Weaver
{
    public class WeaverSettings : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("m_IsEnabled")]
        private bool isEnabled = true;

        [SerializeField, FormerlySerializedAs("m_RequiredScriptingSymbols")]
        [Tooltip("Required DEFINE directives for this settings to be included. Separated with semicolons")]
        private string requiredScriptingSymbols = "";

        [SerializeField, FormerlySerializedAs("m_WeavedAssemblies")]
        private List<WeavedAssembly> weavedAssemblies = new();

        public bool IsEnabled => isEnabled;
        public string RequireScriptingSymbols => requiredScriptingSymbols;
        public List<WeavedAssembly> WeavedAssemblies => weavedAssemblies;

        private void OnEnable() => Debug.Log("[WeaverSettings] OnEnable");
    }
}

