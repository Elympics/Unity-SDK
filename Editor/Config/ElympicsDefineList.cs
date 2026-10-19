using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Elympics.Editor.Config
{
    internal class ElympicsDefineList : ScriptableObject, IEnumerable<ElympicsDefineList.ElympicsDefineDescription>
    {
        [SerializeField] private ElympicsDefineDescription[] list = Array.Empty<ElympicsDefineDescription>();

        [Serializable]
        internal struct ElympicsDefineDescription
        {
            public string Name => name ?? "";
            public string Description => description ?? "";
            /// <summary>Names of the build targets (<see cref="UnityEditor.Build.NamedBuildTarget.TargetName"/>) the define is set for,
            /// whatever the active one. Empty: the active build target.</summary>
            public IReadOnlyList<string> BuildTargets => buildTargets ?? Array.Empty<string>();

            [SerializeField] private string name;
            [SerializeField, TextArea] private string description;
            [Tooltip("NamedBuildTarget names, e.g. Standalone, Server, WebGL, Android, iOS. Leave empty for the active build target.")]
            [SerializeField] private string[] buildTargets;
        }

        public ElympicsDefineDescription this[int index] => list[index];

        public IEnumerator<ElympicsDefineDescription> GetEnumerator() => ((IEnumerable<ElympicsDefineDescription>)list).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
