using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public class ElympicsArray<T> : ElympicsVar where T : ElympicsVar, new()
    {
        [SerializeField] private T[] values;
        public IReadOnlyList<T> Values => values;

        //Parameterless constructor for Unity serialization
        public ElympicsArray() : this(0) { }

        public ElympicsArray(int capacity) : this(capacity, () => new T(), true) { }

        public ElympicsArray(int capacity, Func<T> factory, bool enableSynchronization = true) : base(enableSynchronization) =>
            values = Populate(new T[capacity], factory);

        public ElympicsArray(params T[] elems) : this(true, elems) =>
            values = elems;

        public ElympicsArray(bool enableSynchronization, params T[] elems) : base(enableSynchronization) =>
            values = elems;

        public override void Serialize(BinaryWriter bw)
        {
            foreach (var elympicsVar in values)
                elympicsVar.Serialize(bw);
        }

        public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
        {
            foreach (var elympicsVar in values)
                elympicsVar.Deserialize(br, ignoreTolerance);
        }

        public override bool Equals(BinaryReader br1, BinaryReader br2, out string difference1, out string difference2)
        {
            difference1 = string.Empty;
            difference2 = string.Empty;

            for (var i = 0; i < values.Length; i++)
            {
                if (!values[i].Equals(br1, br2, out var elementDifference1, out var elementDifference2))
                {
#if !ELYMPICS_PRODUCTION
                    difference1 = $"array with value '{elementDifference1}' at index {i}";
                    difference2 = $"array with value '{elementDifference2}' at index {i}";
#endif
                    return false;
                }
            }

            return true;
        }

        internal override void Commit()
        {
            foreach (var elympicsVar in values)
                elympicsVar.Commit();
        }

        private static T[] Populate(T[] array, Func<T> provider)
        {
            for (var i = 0; i < array.Length; i++)
                array[i] = provider();
            return array;
        }

        internal override void Initialize(IElympics elympics)
        {
            base.Initialize(elympics);

            foreach (var element in values)
                element.Initialize(elympics);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            _ = sb.Append($"Array({values.Length}) [");
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
