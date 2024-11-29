using System;
using System.IO;
using Random = Unity.Mathematics.Random;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsRandom : ElympicsVar<ElympicsRandomInternal>
    {
        public override void Serialize(BinaryWriter bw) => bw.Write(Value.Rng.state);

        protected override ElympicsRandomInternal DeserializeInternal(BinaryReader br) => new(br.ReadUInt32());

        public float NextFloat(float minInclusive, float maxInclusive) => Value.Rng.NextFloat(minInclusive, maxInclusive);

        public int NextInt(int minInclusive, int maxInclusive) => Value.Rng.NextInt(minInclusive, maxInclusive);

        public float NextFloat() => Value.Rng.NextFloat();
    }

    public readonly struct ElympicsRandomInternal : IEquatable<ElympicsRandomInternal>
    {
        internal readonly Random Rng;

        internal ElympicsRandomInternal(uint state) => Rng.state = state;

        public bool Equals(ElympicsRandomInternal other) => other.Rng.state == Rng.state;

        public override bool Equals(object obj) => obj is ElympicsRandomInternal other && Equals(other);

        public override int GetHashCode() => Rng.state.GetHashCode();
    }
}
