using System;
using UnityEngine;
using UnityEngine.Serialization;

#nullable enable

namespace Elympics.Editor.Weaving.Settings
{
    /// <summary>
    /// Keeps track of the assembly path and if the
    /// weaving is enabled or not.
    /// </summary>
    [Serializable]
    internal class WeavedAssembly
    {
        [SerializeField, FormerlySerializedAs("m_RelativePath")]
        private string name = "";

        public string Name
        {
            get => name;
            set => name = value;
        }
    }
}
