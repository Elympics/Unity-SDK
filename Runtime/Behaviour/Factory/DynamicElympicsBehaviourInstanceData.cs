using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using MessagePack;

namespace Elympics
{
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [MessagePackObject]
    public struct DynamicElympicsBehaviourInstanceData : IEquatable<DynamicElympicsBehaviourInstanceData>
    {
        [Key(0)] public int ID;
        [Key(1)] public int PrecedingNetworkIdEnumeratorValue;
        [Key(2)] public string InstanceType;

        public DynamicElympicsBehaviourInstanceData(int id, int precedingNetworkIdEnumeratorValue, string instanceType)
        {
            ID = id;
            PrecedingNetworkIdEnumeratorValue = precedingNetworkIdEnumeratorValue;
            InstanceType = instanceType;
        }

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(ID);
            bw.Write(PrecedingNetworkIdEnumeratorValue);
            bw.Write(InstanceType);
            return ms.ToArray();
        }

        public static DynamicElympicsBehaviourInstanceData DeserializeFrom(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            var instanceData = new DynamicElympicsBehaviourInstanceData
            {
                ID = br.ReadInt32(),
                PrecedingNetworkIdEnumeratorValue = br.ReadInt32(),
                InstanceType = br.ReadString()
            };
            return instanceData;
        }

        public override bool Equals(object obj) => obj is DynamicElympicsBehaviourInstanceData data && Equals(data);
        public bool Equals(DynamicElympicsBehaviourInstanceData other) => ID == other.ID && PrecedingNetworkIdEnumeratorValue == other.PrecedingNetworkIdEnumeratorValue && InstanceType == other.InstanceType;
        public override int GetHashCode() => HashCode.Combine(ID, PrecedingNetworkIdEnumeratorValue, InstanceType);
        public override string ToString() => $"({nameof(ID)}: {ID}, {nameof(PrecedingNetworkIdEnumeratorValue)}: {PrecedingNetworkIdEnumeratorValue}, {nameof(InstanceType)}: {InstanceType})";

        public static bool operator ==(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => left.Equals(right);
        public static bool operator !=(DynamicElympicsBehaviourInstanceData left, DynamicElympicsBehaviourInstanceData right) => !(left == right);
    }
}
