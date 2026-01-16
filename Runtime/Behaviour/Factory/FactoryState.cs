#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Utils;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public readonly struct FactoryState : IEquatable<FactoryState>
    {
        [Key(0)] public readonly Dictionary<int, FactoryPartState> Parts;

        [SerializationConstructor]
        public FactoryState(Dictionary<int, FactoryPartState> parts) => Parts = parts ?? throw new ArgumentNullException(nameof(parts));

        public override bool Equals(object obj) => obj is FactoryState other && Equals(other);
        public bool Equals(FactoryState other) => Parts.SequenceEqualNullable(other.Parts);
        public override int GetHashCode() => HashCode.Combine(Parts.Count);

        public static bool operator ==(FactoryState left, FactoryState right) => EqualityComparer<FactoryState>.Default.Equals(left, right);
        public static bool operator !=(FactoryState left, FactoryState right) => !(left == right);
    }

    [MessagePackObject]
    public readonly struct FactoryPartState : IEquatable<FactoryPartState>
    {
        [Key(0)] public readonly int CurrentNetworkId;
        [Key(1)] public readonly DynamicElympicsBehaviourInstancesDataState DynamicInstancesState;

        public FactoryPartState(int currentNetworkId, DynamicElympicsBehaviourInstancesDataState dynamicInstancesState)
        {
            CurrentNetworkId = currentNetworkId;
            DynamicInstancesState = dynamicInstancesState;
        }

        public override bool Equals(object obj) => obj is FactoryPartState state && Equals(state);
        public bool Equals(FactoryPartState other) => CurrentNetworkId == other.CurrentNetworkId && DynamicInstancesState.Equals(other.DynamicInstancesState);
        public override int GetHashCode() => HashCode.Combine(CurrentNetworkId, DynamicInstancesState);
        public override string ToString() => $"({nameof(CurrentNetworkId)}: {CurrentNetworkId}, {nameof(DynamicInstancesState)}: {DynamicInstancesState})";

        public static bool operator ==(FactoryPartState left, FactoryPartState right) => left.Equals(right);
        public static bool operator !=(FactoryPartState left, FactoryPartState right) => !(left == right);
    }

    [MessagePackObject]
    public readonly struct DynamicElympicsBehaviourInstancesDataState : IEquatable<DynamicElympicsBehaviourInstancesDataState>
    {
        [Key(0)] public readonly int InstancesCounter;
        [Key(1)] public readonly Dictionary<int, DynamicElympicsBehaviourInstanceData> Instances;

        public DynamicElympicsBehaviourInstancesDataState(int instancesCounter, Dictionary<int, DynamicElympicsBehaviourInstanceData> instances)
        {
            InstancesCounter = instancesCounter;
            Instances = instances ?? throw new ArgumentNullException(nameof(instances));
        }

        public override bool Equals(object obj) => obj is DynamicElympicsBehaviourInstancesDataState state && Equals(state);
        public bool Equals(DynamicElympicsBehaviourInstancesDataState other) => InstancesCounter == other.InstancesCounter && Instances.SequenceEqualNullable(other.Instances);
        public override int GetHashCode() => HashCode.Combine(InstancesCounter, Instances.Count);
        public override string ToString() => $"({nameof(InstancesCounter)}: {InstancesCounter}, {nameof(Instances)}: [{Instances.Select(x => $"{{{x.Key}, {x.Value}}}").CommaList()}])";

        public static bool operator ==(DynamicElympicsBehaviourInstancesDataState left, DynamicElympicsBehaviourInstancesDataState right) => left.Equals(right);
        public static bool operator !=(DynamicElympicsBehaviourInstancesDataState left, DynamicElympicsBehaviourInstancesDataState right) => !(left == right);
    }
}
