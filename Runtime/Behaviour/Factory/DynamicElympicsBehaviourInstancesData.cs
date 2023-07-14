using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Elympics
{
    internal class DynamicElympicsBehaviourInstancesData : ElympicsVar
    {
        private int _instancesCounter;
        private readonly Dictionary<int, byte[]> _instancesSerialized = new();
        private readonly Dictionary<int, byte[]> _incomingInstancesSerialized = new();

        private readonly List<(int, byte[])> _instancesToRemoveSerialized = new();
        private readonly List<(int, byte[])> _instancesToAddSerialized = new();

        private readonly Dictionary<int, byte[]> _equalsInstancesSerialized1 = new();
        private readonly Dictionary<int, byte[]> _equalsInstancesSerialized2 = new();

        public DynamicElympicsBehaviourInstancesData(int instancesCounterStart) : base(true)
        {
            _instancesCounter = instancesCounterStart;
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(_instancesCounter);
            bw.Write(_instancesSerialized);
        }

        public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
        {
            _instancesCounter = DeserializeInternalInstancesCounter(br);
            DeserializeInternalIncomingInstances(br, _incomingInstancesSerialized);
            CalculateIncomingInstancesDiff(_instancesSerialized, _incomingInstancesSerialized);
        }

        private static int DeserializeInternalInstancesCounter(BinaryReader br) => br.ReadInt32();

        private void DeserializeInternalIncomingInstances(BinaryReader br, Dictionary<int, byte[]> incomingInstancesSerialized)
        {
            incomingInstancesSerialized.Clear();
            br.ReadIntoDictionaryIntToByteArray(incomingInstancesSerialized);
        }

        private void CalculateIncomingInstancesDiff(Dictionary<int, byte[]> instancesSerialized, Dictionary<int, byte[]> incomingInstancesSerialized)
        {
            _instancesToRemoveSerialized.Clear();
            _instancesToAddSerialized.Clear();

            foreach (var (instanceId, incomingInstanceData) in incomingInstancesSerialized)
            {
                if (instancesSerialized.TryGetValue(instanceId, out var currentInstanceData))
                {
                    if (currentInstanceData.SequenceEqual(incomingInstanceData))
                        continue;

                    _instancesToRemoveSerialized.Add((instanceId, currentInstanceData));
                    _instancesToAddSerialized.Add((instanceId, incomingInstanceData));
                }
                else
                {
                    _instancesToAddSerialized.Add((instanceId, incomingInstanceData));
                }
            }

            foreach (var (instanceId, currentInstanceData) in instancesSerialized)
            {
                if (!incomingInstancesSerialized.ContainsKey(instanceId))
                    _instancesToRemoveSerialized.Add((instanceId, currentInstanceData));
            }
        }

        internal bool AreIncomingInstancesTheSame() => _instancesToAddSerialized.Count == 0 && _instancesToRemoveSerialized.Count == 0;

        internal (IEnumerable<InstanceData> instancesToRemove, IEnumerable<InstanceData> instancesToAdd) GetIncomingDiff() =>
            (_instancesToRemoveSerialized.Select(x => InstanceData.DeserializeFrom(x.Item2)),
                _instancesToAddSerialized.Select(x => InstanceData.DeserializeFrom(x.Item2)));

        internal void ApplyIncomingInstances()
        {
            // Important - first remove obsolete instances as there could be same ids but other instance content, then add new
            foreach (var (instanceId, _) in _instancesToRemoveSerialized)
                _ = _instancesSerialized.Remove(instanceId);

            foreach (var (instanceId, instanceDataSerialized) in _instancesToAddSerialized)
                _instancesSerialized.Add(instanceId, instanceDataSerialized);
        }

        internal void Remove(int instanceId)
        {
            _ = _instancesSerialized.Remove(instanceId);
        }

        internal int Add(int precedingNetworkIdEnumeratorValue, string pathInResources)
        {
            var instanceId = _instancesCounter;
            _instancesCounter++;
            _instancesSerialized.Add(instanceId, new InstanceData(instanceId, precedingNetworkIdEnumeratorValue, pathInResources).Serialize());
            return instanceId;
        }

        internal int Count => _instancesSerialized.Count;

        public override bool Equals(BinaryReader br1, BinaryReader br2)
        {
            var instancesCounter1 = DeserializeInternalInstancesCounter(br1);
            var instancesCounter2 = DeserializeInternalInstancesCounter(br2);

            DeserializeInternalIncomingInstances(br1, _equalsInstancesSerialized1);
            DeserializeInternalIncomingInstances(br2, _equalsInstancesSerialized2);

            if (instancesCounter1 != instancesCounter2)
                return false;

            CalculateIncomingInstancesDiff(_equalsInstancesSerialized1, _equalsInstancesSerialized2);
            return AreIncomingInstancesTheSame();
        }

        internal override void Commit()
        { }

        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
        public struct InstanceData
        {
            public int ID;
            public int PrecedingNetworkIdEnumeratorValue;
            public string InstanceType;

            public InstanceData(int id, int precedingNetworkIdEnumeratorValue, string instanceType)
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

            public static InstanceData DeserializeFrom(byte[] data)
            {
                using var ms = new MemoryStream(data);
                using var br = new BinaryReader(ms);
                var instanceData = new InstanceData
                {
                    ID = br.ReadInt32(),
                    PrecedingNetworkIdEnumeratorValue = br.ReadInt32(),
                    InstanceType = br.ReadString()
                };
                return instanceData;
            }
        }
    }
}
