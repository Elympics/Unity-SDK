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
        
        public void SetState(uint state) => Value = new(state);

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            var rng = Value.Rng;
            var result = rng.NextFloat(minInclusive, maxInclusive);
            Value = new(rng.state);

            return result;
        }

        public int NextInt(int minInclusive, int maxInclusive)
        {
            var rng = Value.Rng;
            var result = rng.NextInt(minInclusive, maxInclusive);
            Value = new(rng.state);

            return result;
        }

        public float NextFloat()
        {
            var rng = Value.Rng;
            var result = rng.NextFloat();
            Value = new(rng.state);

            return result;
        }
    }

    public struct ElympicsRandomInternal : IEquatable<ElympicsRandomInternal>
    {
        internal Random Rng;

        internal ElympicsRandomInternal(uint state) => Rng.state = state;

        public bool Equals(ElympicsRandomInternal other) => other.Rng.state == Rng.state;

        public override bool Equals(object obj) => obj is ElympicsRandomInternal other && Equals(other);

        public override int GetHashCode() => Rng.state.GetHashCode();
    }
}
