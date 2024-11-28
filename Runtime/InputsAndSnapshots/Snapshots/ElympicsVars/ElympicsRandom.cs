using System;
using System.IO;
using Random = Unity.Mathematics.Random;

namespace Elympics
{
    [Serializable]
    public class ElympicsRandom : ElympicsVar<ElympicsRandomInternal>, IRng
    {
        public override void Serialize(BinaryWriter bw) => bw.Write(Value.rng.state);

        protected override ElympicsRandomInternal DeserializeInternal(BinaryReader br) => new(br.ReadUInt32());

        public float Range(float minInclusive, float maxInclusive) => Value.rng.NextFloat(minInclusive, maxInclusive);

        public int Range(int minInclusive, int maxInclusive) => Value.rng.NextInt(minInclusive, maxInclusive);

        public float value => Value.rng.NextFloat();
    }

    public readonly struct ElympicsRandomInternal : IEquatable<ElympicsRandomInternal>
    {
        public readonly Random rng;

        public ElympicsRandomInternal(uint state) => this.rng.state = state;

        public bool Equals(ElympicsRandomInternal other) => other.rng.state == this.rng.state;

        public override bool Equals(object obj) => obj is ElympicsRandomInternal other && Equals(other);

        public override int GetHashCode() => rng.GetHashCode();

        public static implicit operator Random(ElympicsRandomInternal random) => random.rng;
        public static implicit operator ElympicsRandomInternal(Random random) => new(random.state);
    }

    public interface IRng
    {
        float Range(float minInclusive, float maxInclusive);
        int Range(int minInclusive, int maxInclusive);
        float value { get; }
    }

    public class UnityRandom : IRng
    {
        public float Range(float minInclusive, float maxInclusive) => UnityEngine.Random.Range(minInclusive, maxInclusive);
        public int Range(int minInclusive, int maxInclusive) => UnityEngine.Random.Range(minInclusive, maxInclusive);
        public float value => UnityEngine.Random.value;
    }
}
