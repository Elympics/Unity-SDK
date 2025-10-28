using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public class ElympicsList<T> : ElympicsVar, IEnumerable<T> where T : ElympicsVar, new()
    {
        private Func<T> _factoryFunc;
        [SerializeField] private List<T> values;

        private T _listElementInstanceForDataCompare;

        public int Count => values.Count;

        public bool IsReadOnly => false;

        public T this[int index] => values[index];

        //Parameterless constructor for Unity serialization
        public ElympicsList() : this(() => new T(), 0, true) { }

        public ElympicsList(Func<T> factory, int elementsInListAtStart = 0, bool enableSynchronization = true) : base(enableSynchronization)
        {
            _factoryFunc = factory;
            _listElementInstanceForDataCompare = factory();

            values = new List<T>();
            AddElementsToList(elementsInListAtStart);
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(values.Count);

            foreach (var elympicsVar in values)
                elympicsVar.Serialize(bw);
        }

        public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
        {
            var elementsInList = br.ReadInt32();

            if (elementsInList > values.Count)
                AddElementsToList(elementsInList - values.Count);
            else if (elementsInList < values.Count)
                RemoveElementsFromList(values.Count - elementsInList);

            foreach (var elympicsVar in values)
                elympicsVar.Deserialize(br, ignoreTolerance);
        }

        private void AddElementsToList(int elementsToAdd)
        {
            for (var i = 0; i < elementsToAdd; i++)
                _ = Add();
        }

        private void RemoveElementsFromList(int elementsToRemove)
        {
            values.RemoveRange(values.Count - elementsToRemove, elementsToRemove);
        }

        public override bool Equals(BinaryReader br1, BinaryReader br2, out string difference1, out string difference2)
        {
            difference1 = string.Empty;
            difference2 = string.Empty;
            var list1Length = br1.ReadInt32();
            var list2Length = br2.ReadInt32();

            if (list1Length != list2Length)
            {
#if !ELYMPICS_PRODUCTION
                difference1 = $"list with length of {list1Length}";
                difference2 = $"list with length of {list2Length}";
#endif
                return false;
            }

            for (var i = 0; i < list1Length; i++)
                if (!_listElementInstanceForDataCompare.Equals(br1, br2, out var elementDifference1, out var elementDifference2))
                {
#if !ELYMPICS_PRODUCTION
                    difference1 = $"list with length of {list1Length} and value '{elementDifference1}' at index {i}";
                    difference2 = $"list with length of {list2Length} and value '{elementDifference2}' at index {i}";
#endif
                    return false;
                }

            return true;
        }

        internal override void Commit()
        {
            foreach (var elympicsVar in values)
                elympicsVar.Commit();
        }

        public int IndexOf(T item)
        {
            return values.IndexOf(item);
        }

        public T Insert(int index)
        {
            var createdInstance = CreateAndInitialize();
            values.Insert(index, createdInstance);

            return createdInstance;
        }

        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
        }

        public T Add()
        {
            var createdInstance = CreateAndInitialize();
            values.Add(createdInstance);

            return createdInstance;
        }

        private T CreateAndInitialize()
        {
            var createdInstance = _factoryFunc();
            createdInstance.Initialize(Elympics);
            return createdInstance;
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool Contains(T item)
        {
            return values.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return values.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        internal override void Initialize(IElympics elympics)
        {
            base.Initialize(elympics);
            _listElementInstanceForDataCompare.Initialize(elympics);

            foreach (var element in values)
                element.Initialize(elympics);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            _ = sb.Append($"List({Count}) [");
            var first = true;
            foreach (var elympicsVar in values)
            {
                if (!first)
                    _ = sb.Append(", ");
                first = false;
                _ = sb.Append(elympicsVar);
            }

            _ = sb.Append("]");
            return sb.ToString();
        }
    }
}
