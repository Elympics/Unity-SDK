using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Utils;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class FactoryState : IEquatable<FactoryState>
    {
        [Key(1)] public List<KeyValuePair<int, FactoryPartState>> Parts;

        public override bool Equals(object obj) => Equals(obj as FactoryState);
        public bool Equals(FactoryState other) => other is not null && Parts.SequenceEqualNullable(other.Parts);
        public override int GetHashCode() => HashCode.Combine(Parts.Count);

        public static bool operator ==(FactoryState left, FactoryState right) => EqualityComparer<FactoryState>.Default.Equals(left, right);
        public static bool operator !=(FactoryState left, FactoryState right) => !(left == right);
    }

    [MessagePackObject]
    public struct FactoryPartState : IEquatable<FactoryPartState>
    {
        [Key(0)] public int currentNetworkId;
        [Key(1)] public DynamicElympicsBehaviourInstancesDataState dynamicInstancesState;

        public override bool Equals(object obj) => obj is FactoryPartState state && Equals(state);
        public bool Equals(FactoryPartState other) => currentNetworkId == other.currentNetworkId && dynamicInstancesState.Equals(other.dynamicInstancesState);
        public override int GetHashCode() => HashCode.Combine(currentNetworkId, dynamicInstancesState);
        public override string ToString() => $"({nameof(currentNetworkId)}: {currentNetworkId}, {nameof(dynamicInstancesState)}: {dynamicInstancesState})";

        public static bool operator ==(FactoryPartState left, FactoryPartState right) => left.Equals(right);
        public static bool operator !=(FactoryPartState left, FactoryPartState right) => !(left == right);
    }

    [MessagePackObject]
    public struct DynamicElympicsBehaviourInstancesDataState : IEquatable<DynamicElympicsBehaviourInstancesDataState>
    {
        [Key(0)] public int instancesCounter;
        [Key(1)] public Dictionary<int, DynamicElympicsBehaviourInstanceData> instances;

        public override bool Equals(object obj) => obj is DynamicElympicsBehaviourInstancesDataState state && Equals(state);
        public bool Equals(DynamicElympicsBehaviourInstancesDataState other) => instancesCounter == other.instancesCounter && instances.SequenceEqualNullable(other.instances);
        public override int GetHashCode() => HashCode.Combine(instancesCounter, instances.Count);
        public override string ToString() => $"({nameof(instancesCounter)}: {instancesCounter}, {nameof(instances)}: [{instances.Select(x => $"{{{x.Key}, {x.Value}}}").CommaList()}])";

        public static bool operator ==(DynamicElympicsBehaviourInstancesDataState left, DynamicElympicsBehaviourInstancesDataState right) => left.Equals(right);
        public static bool operator !=(DynamicElympicsBehaviourInstancesDataState left, DynamicElympicsBehaviourInstancesDataState right) => !(left == right);
    }
}
