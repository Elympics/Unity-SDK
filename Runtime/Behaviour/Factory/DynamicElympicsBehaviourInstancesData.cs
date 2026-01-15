using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elympics
{
    internal class DynamicElympicsBehaviourInstancesData
    {
        private int _instancesCounter;
        private readonly Dictionary<int, byte[]> _instancesSerialized = new();
        private readonly Dictionary<int, byte[]> _incomingInstancesSerialized = new();

        private readonly List<(int, byte[])> _instancesToRemoveSerialized = new();
        private readonly List<(int, byte[])> _instancesToAddSerialized = new();

        private readonly Dictionary<int, byte[]> _equalsInstancesSerializedhistory = new();
        private readonly Dictionary<int, byte[]> _equalsInstancesSerializedreceived = new();

        public DynamicElympicsBehaviourInstancesData(int instancesCounterStart) => _instancesCounter = instancesCounterStart;

        public DynamicElympicsBehaviourInstancesDataState GetState()
        {
            return new()
            {
                instancesCounter = _instancesCounter,
                instances = new(_instancesSerialized.Select(kvp => new KeyValuePair<int, DynamicElympicsBehaviourInstanceData>(kvp.Key, DynamicElympicsBehaviourInstanceData.DeserializeFrom(kvp.Value))))
            };
        }

        public void ApplyState(DynamicElympicsBehaviourInstancesDataState data)
        {
            _instancesCounter = data.instancesCounter;

            _incomingInstancesSerialized.Clear();
            foreach ((var key, var value) in data.instances)
            {
                var serializedValue = value.Serialize();
                _incomingInstancesSerialized.Add(key, serializedValue);
            }

            CalculateIncomingInstancesDiff(_instancesSerialized, _incomingInstancesSerialized);
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

        internal (IEnumerable<DynamicElympicsBehaviourInstanceData> instancesToRemove, IEnumerable<DynamicElympicsBehaviourInstanceData> instancesToAdd) GetIncomingDiff() =>
            (_instancesToRemoveSerialized.Select(x => DynamicElympicsBehaviourInstanceData.DeserializeFrom(x.Item2)),
                _instancesToAddSerialized.Select(x => DynamicElympicsBehaviourInstanceData.DeserializeFrom(x.Item2)));

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
            _instancesSerialized.Add(instanceId, new DynamicElympicsBehaviourInstanceData(instanceId, precedingNetworkIdEnumeratorValue, pathInResources).Serialize());
            return instanceId;
        }

        internal int Count => _instancesSerialized.Count;

        public bool Equals(FactoryPartState historyPartState, FactoryPartState receivedPartState, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var historyInstancesCount = historyPartState.dynamicInstancesState.instancesCounter;
            var receivedInstancesCount = receivedPartState.dynamicInstancesState.instancesCounter;

            _equalsInstancesSerializedhistory.Clear();
            foreach ((var key, var value) in historyPartState.dynamicInstancesState.instances)
                _equalsInstancesSerializedhistory.Add(key, value.Serialize());

            _equalsInstancesSerializedreceived.Clear();
            foreach ((var key, var value) in receivedPartState.dynamicInstancesState.instances)
                _equalsInstancesSerializedreceived.Add(key, value.Serialize());

            if (historyInstancesCount != receivedInstancesCount)
            {
#if !ELYMPICS_PRODUCTION
                ElympicsLogger.LogWarning($"The number of dynamic object instances for player {player} in local snapshot history for tick {historyTick} doesn't match that received from the game server. " +
            $"Number in local history: {historyInstancesCount} received number: {receivedInstancesCount}. " +
            $"Last simulated tick: {lastSimulatedTick}." +
            $"This means that the client incorrectly predicted spawning/destruction of objects.");
#endif
                return false;
            }

            CalculateIncomingInstancesDiff(_equalsInstancesSerializedhistory, _equalsInstancesSerializedreceived);
            var areInstancesTheSame = AreIncomingInstancesTheSame();

#if !ELYMPICS_PRODUCTION
            if (!areInstancesTheSame)
            {
                var sb = new StringBuilder();
                _ = sb.Append("The dynamic object instances for player ").Append(player)
                    .Append(" in local snapshot history for tick ").Append(historyTick).Append(" don't match those received from the game server. ")
                    .Append("Last simulated tick: ").Append(lastSimulatedTick).Append(". ");

                if (_instancesToAddSerialized.Count > 0)
                    _ = sb.Append($"Number of instances missing in local history: ").Append(_instancesToAddSerialized.Count).Append(". ")
                        .Append($"Client either didn't predict that those instances should be spawned or incorrectly predicted that they should be destroyed. ");

                if (_instancesToRemoveSerialized.Count > 0)
                    _ = sb.Append($"Number of instances that don't exist in the received state, but are present in local history: ")
                        .Append(_instancesToRemoveSerialized.Count).Append(". ")
                        .Append($"Client either didn't predict that those instances should be destoryed or incorrectly predicted that they should be spawned.");

                ElympicsLogger.LogWarning(sb.ToString());
            }
#endif

            return areInstancesTheSame;
        }
    }
}
