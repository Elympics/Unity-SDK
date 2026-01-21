using System;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public readonly struct DynamicElympicsBehaviourInstanceData : IEquatable<DynamicElympicsBehaviourInstanceData>
    {
        [Key(0)] public readonly int ID;
        [Key(1)] public readonly int PrecedingNetworkIdEnumeratorValue;
        [Key(2)] public readonly string InstanceType;

        public DynamicElympicsBehaviourInstanceData(int id, int precedingNetworkIdEnumeratorValue, string instanceType)
        {
            ID = id;
            PrecedingNetworkIdEnumeratorValue = precedingNetworkIdEnumeratorValue;
            InstanceType = instanceType;
        }

        public override bool Equals(object obj) => obj is DynamicElympicsBehaviourInstanceData data && Equals(data);
        public bool Equals(DynamicElympicsBehaviourInstanceData other) => ID == other.ID && PrecedingNetworkIdEnumeratorValue == other.PrecedingNetworkIdEnumeratorValue && InstanceType == other.InstanceType;
        public override int GetHashCode() => HashCode.Combine(ID, PrecedingNetworkIdEnumeratorValue, InstanceType);
        public override string ToString() => $"({nameof(ID)}: {ID}, {nameof(PrecedingNetworkIdEnumeratorValue)}: {PrecedingNetworkIdEnumeratorValue}, {nameof(InstanceType)}: {InstanceType})";

        public static bool operator ==(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => left.Equals(right);
        public static bool operator !=(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => !(left == right);
    }
}
