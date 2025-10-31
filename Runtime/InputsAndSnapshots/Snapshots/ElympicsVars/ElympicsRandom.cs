using System;
using System.IO;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Elympics
{
    /// <summary>
    /// Wrapper for <see cref="Random"/> that synchronizes its state as <see cref="ElympicsVar"/>.
    /// Always call <see cref="SetSeed"/> to initialize state before use.
    /// </summary>
    /// <remarks>
    /// This PRNG is not cryptographically secure and should never be used as a substitute for a CSPRNG.
    /// </remarks>
    [Serializable]
    public sealed class ElympicsRandom : ElympicsVar
    {
        private Random _random;

        public override void Serialize(BinaryWriter bw) => bw.Write(_random.state);

        public override void Deserialize(BinaryReader br, bool ignoreTolerance = false) => _random.state = br.ReadUInt32();
        public override bool Equals(BinaryReader br1, BinaryReader br2, out string difference1, out string difference2)
        {
            difference1 = string.Empty;
            difference2 = string.Empty;
            var value1 = br1.ReadUInt32();
            var value2 = br2.ReadUInt32();
            var areEqual = value1 == value2;

#if !ELYMPICS_PRODUCTION
            if (!areEqual)
            {
                difference1 = $"rng with internal state {value1}";
                difference2 = $"rng with internal state {value2}";
            }
#endif

            return areEqual;
        }

        internal override void Commit() { }

        /// <summary>Always call this method to set a seed before using this class.</summary>
        /// <param name="seed">Seed for RNG. This value must be non-zero.</param>
        public void SetSeed(uint seed) => _random = new Random(seed);

        /// <summary>Using this constructor removes the need to call <see cref="SetSeed"/>.</summary>
        /// <param name="seed">Seed for RNG. This value must be non-zero.</param>
        public ElympicsRandom(uint seed, bool enabledSynchronization = true) : this(enabledSynchronization) => SetSeed(seed);

        public ElympicsRandom(bool enabledSynchronization = true) : base(enabledSynchronization) { }

        #region public RNG methods

        /// <inheritdoc cref="Random.NextBool()"/>
        public bool NextBool() => _random.NextBool();
        /// <inheritdoc cref="Random.NextBool2()"/>
        public bool2 NextBool2() => _random.NextBool2();
        /// <inheritdoc cref="Random.NextBool3()"/>
        public bool3 NextBool3() => _random.NextBool3();
        /// <inheritdoc cref="Random.NextBool4()"/>
        public bool4 NextBool4() => _random.NextBool4();
        /// <inheritdoc cref="Random.NextDouble()"/>
        public double NextDouble() => _random.NextDouble();
        /// <inheritdoc cref="Random.NextDouble(double)"/>
        public double NextDouble(double maxExclusive) => _random.NextDouble(maxExclusive);
        /// <inheritdoc cref="Random.NextDouble(double, double)"/>
        public double NextDouble(double minInclusive, double maxExclusive) => _random.NextDouble(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextDouble2()"/>
        public double2 NextDouble2() => _random.NextDouble2();
        /// <inheritdoc cref="Random.NextDouble2Direction()"/>
        public double2 NextDouble2Direction() => _random.NextDouble2Direction();
        /// <inheritdoc cref="Random.NextDouble2(double2)"/>
        public double2 NextDouble2(double2 maxExclusive) => _random.NextDouble2(maxExclusive);
        /// <inheritdoc cref="Random.NextDouble2(double2, double2)"/>
        public double2 NextDouble2(double2 minInclusive, double2 maxExclusive) => _random.NextDouble2(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextDouble3()"/>
        public double3 NextDouble3() => _random.NextDouble3();
        /// <inheritdoc cref="Random.NextDouble3Direction()"/>
        public double3 NextDouble3Direction() => _random.NextDouble3Direction();
        /// <inheritdoc cref="Random.NextDouble3(double3)"/>
        public double3 NextDouble3(double3 maxExclusive) => _random.NextDouble3(maxExclusive);
        /// <inheritdoc cref="Random.NextDouble3(double3, double3)"/>
        public double3 NextDouble3(double3 minInclusive, double3 maxExclusive) => _random.NextDouble3(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextDouble4()"/>
        public double4 NextDouble4() => _random.NextDouble4();
        /// <inheritdoc cref="Random.NextDouble4(double4)"/>
        public double4 NextDouble4(double4 maxExclusive) => _random.NextDouble4(maxExclusive);
        /// <inheritdoc cref="Random.NextDouble4(double4, double4)"/>
        public double4 NextDouble4(double4 minInclusive, double4 maxExclusive) => _random.NextDouble4(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextFloat()"/>
        public float NextFloat() => _random.NextFloat();
        /// <inheritdoc cref="Random.NextFloat(float)"/>
        public float NextFloat(float maxExclusive) => _random.NextFloat(maxExclusive);
        /// <inheritdoc cref="Random.NextFloat(float, float)"/>
        public float NextFloat(float minInclusive, float maxExclusive) => _random.NextFloat(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextFloat2()"/>
        public float2 NextFloat2() => _random.NextFloat2();
        /// <inheritdoc cref="Random.NextFloat2Direction()"/>
        public float2 NextFloat2Direction() => _random.NextFloat2Direction();
        /// <inheritdoc cref="Random.NextFloat2(float2)"/>
        public float2 NextFloat2(float2 maxExclusive) => _random.NextFloat2(maxExclusive);
        /// <inheritdoc cref="Random.NextFloat2(float2, float2)"/>
        public float2 NextFloat2(float2 minInclusive, float2 maxExclusive) => _random.NextFloat2(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextFloat3()"/>
        public float3 NextFloat3() => _random.NextFloat3();
        /// <inheritdoc cref="Random.NextFloat3Direction()"/>
        public float3 NextFloat3Direction() => _random.NextFloat3Direction();
        /// <inheritdoc cref="Random.NextFloat3(float3)"/>
        public float3 NextFloat3(float3 maxExclusive) => _random.NextFloat3(maxExclusive);
        /// <inheritdoc cref="Random.NextFloat3(float3, float3)"/>
        public float3 NextFloat3(float3 minInclusive, float3 maxExclusive) => _random.NextFloat3(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextFloat4()"/>
        public float4 NextFloat4() => _random.NextFloat4();
        /// <inheritdoc cref="Random.NextFloat4(float4)"/>
        public float4 NextFloat4(float4 maxExclusive) => _random.NextFloat4(maxExclusive);
        /// <inheritdoc cref="Random.NextFloat4(float4, float4)"/>
        public float4 NextFloat4(float4 minInclusive, float4 maxExclusive) => _random.NextFloat4(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextInt()"/>
        public int NextInt() => _random.NextInt();
        /// <inheritdoc cref="Random.NextInt(int)"/>
        public int NextInt(int maxExclusive) => _random.NextInt(maxExclusive);
        /// <inheritdoc cref="Random.NextInt(int, int)"/>
        public int NextInt(int minInclusive, int maxExclusive) => _random.NextInt(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextInt2()"/>
        public int2 NextInt2() => _random.NextInt2();
        /// <inheritdoc cref="Random.NextInt2(int2)"/>
        public int2 NextInt2(int2 maxExclusive) => _random.NextInt2(maxExclusive);
        /// <inheritdoc cref="Random.NextInt2(int2, int2)"/>
        public int2 NextInt2(int2 minInclusive, int2 maxExclusive) => _random.NextInt2(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextInt3()"/>
        public int3 NextInt3() => _random.NextInt3();
        /// <inheritdoc cref="Random.NextInt3(int3)"/>
        public int3 NextInt3(int3 maxExclusive) => _random.NextInt3(maxExclusive);
        /// <inheritdoc cref="Random.NextInt3(int3, int3)"/>
        public int3 NextInt3(int3 minInclusive, int3 maxExclusive) => _random.NextInt3(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextInt4()"/>
        public int4 NextInt4() => _random.NextInt4();
        /// <inheritdoc cref="Random.NextInt4(int4)"/>
        public int4 NextInt4(int4 maxExclusive) => _random.NextInt4(maxExclusive);
        /// <inheritdoc cref="Random.NextInt4(int4, int4)"/>
        public int4 NextInt4(int4 minInclusive, int4 maxExclusive) => _random.NextInt4(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextUInt()"/>
        public uint NextUInt() => _random.NextUInt();
        /// <inheritdoc cref="Random.NextUInt(uint)"/>
        public uint NextUInt(uint maxExclusive) => _random.NextUInt(maxExclusive);
        /// <inheritdoc cref="Random.NextUInt(uint, uint)"/>
        public uint NextUInt(uint minInclusive, uint maxExclusive) => _random.NextUInt(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextUInt2()"/>
        public uint2 NextUInt2() => _random.NextUInt2();
        /// <inheritdoc cref="Random.NextUInt2(uint2)"/>
        public uint2 NextUInt2(uint2 maxExclusive) => _random.NextUInt2(maxExclusive);
        /// <inheritdoc cref="Random.NextUInt2(uint2, uint2)"/>
        public uint2 NextUInt2(uint2 minInclusive, uint2 maxExclusive) => _random.NextUInt2(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextUInt3()"/>
        public uint3 NextUInt3() => _random.NextUInt3();
        /// <inheritdoc cref="Random.NextUInt3(uint3)"/>
        public uint3 NextUInt3(uint3 maxExclusive) => _random.NextUInt3(maxExclusive);
        /// <inheritdoc cref="Random.NextUInt3(uint3, uint3)"/>
        public uint3 NextUInt3(uint3 minInclusive, uint3 maxExclusive) => _random.NextUInt3(minInclusive, maxExclusive);
        /// <inheritdoc cref="Random.NextUInt4()"/>
        public uint4 NextUInt4() => _random.NextUInt4();
        /// <inheritdoc cref="Random.NextUInt4(uint4)"/>
        public uint4 NextUInt4(uint4 maxExclusive) => _random.NextUInt4(maxExclusive);
        /// <inheritdoc cref="Random.NextUInt4(uint4, uint4)"/>
        public uint4 NextUInt4(uint4 minInclusive, uint4 maxExclusive) => _random.NextUInt4(minInclusive, maxExclusive);

        #endregion
    }
}
