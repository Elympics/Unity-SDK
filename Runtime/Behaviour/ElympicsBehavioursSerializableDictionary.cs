using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elympics
{
    [Serializable]
    public class ElympicsBehavioursSerializableDictionary : SortedDictionary<int, ElympicsBehaviour>
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ElympicsBehavioursSerializableDictionary))]
        internal class Drawer : DictionaryDrawer<ElympicsBehavioursSerializableDictionary, int, ElympicsBehaviour>
        {
        }
#endif
    }
}
