using System;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public readonly struct DynamicElympicsBehaviourInstanceData : IEquatable<DynamicElympicsBehaviourInstanceData>
    {
        [Key(0)] public readonly int ID;
        /// <summary>
        /// NetworkIds assigned to each ElympicsBehaviour on the spawned prefab instance.
        /// Stored by reference — callers must not mutate the array after construction.
        /// </summary>
        [Key(1)] public readonly int[] NetworkIds;
        [Key(2)] public readonly string InstanceType;

        [SerializationConstructor]
        public DynamicElympicsBehaviourInstanceData(int id, int[] networkIds, string instanceType)
        {
            ID = id;
            NetworkIds = networkIds ?? throw new ArgumentNullException(nameof(networkIds));
            InstanceType = instanceType;
        }

        public override bool Equals(object obj) => obj is DynamicElympicsBehaviourInstanceData data && Equals(data);

        public bool Equals(DynamicElympicsBehaviourInstanceData other)
        {
            if (ID != other.ID || InstanceType != other.InstanceType)
                return false;
            if (NetworkIds == other.NetworkIds)
                return true;
            if (NetworkIds.Length != other.NetworkIds.Length)
                return false;
            for (var i = 0; i < NetworkIds.Length; i++)
                if (NetworkIds[i] != other.NetworkIds[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(ID, InstanceType);
            for (var i = 0; i < NetworkIds.Length; i++)
                hash = HashCode.Combine(hash, NetworkIds[i]);
            return hash;
        }

        public override string ToString() => $"({nameof(ID)}: {ID}, {nameof(NetworkIds)}: [{string.Join(", ", NetworkIds)}], {nameof(InstanceType)}: {InstanceType})";

        public static bool operator ==(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => left.Equals(right);
        public static bool operator !=(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => !(left == right);
    }
}
