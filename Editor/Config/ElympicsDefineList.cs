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

            [SerializeField] private string name;
            [SerializeField] private string description;
        }

        public ElympicsDefineDescription this[int index] => list[index];

        public IEnumerator<ElympicsDefineDescription> GetEnumerator() => ((IEnumerable<ElympicsDefineDescription>)list).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
