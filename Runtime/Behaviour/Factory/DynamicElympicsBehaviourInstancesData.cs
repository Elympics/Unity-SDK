using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elympics
{
    internal class DynamicElympicsBehaviourInstancesData
    {
        private int _instancesCounter;
        private readonly Dictionary<int, DynamicElympicsBehaviourInstanceData> _instances = new();
        private readonly Dictionary<int, DynamicElympicsBehaviourInstanceData> _incomingInstances = new();

        private readonly List<(int, DynamicElympicsBehaviourInstanceData)> _instancesToRemove = new();
        private readonly List<(int, DynamicElympicsBehaviourInstanceData)> _instancesToAdd = new();

        private readonly Dictionary<int, DynamicElympicsBehaviourInstanceData> _equalsInstancesHistory = new();
        private readonly Dictionary<int, DynamicElympicsBehaviourInstanceData> _equalsInstancesReceived = new();

        public DynamicElympicsBehaviourInstancesData(int instancesCounterStart) => _instancesCounter = instancesCounterStart;

        public DynamicElympicsBehaviourInstancesDataState GetState() => new(_instancesCounter, new(_instances));

        public void ApplyState(DynamicElympicsBehaviourInstancesDataState data)
        {
            _instancesCounter = data.InstancesCounter;

            _incomingInstances.Clear();
            foreach ((var key, var value) in data.Instances)
            {
                _incomingInstances.Add(key, value);
            }

            CalculateIncomingInstancesDiff(_instances, _incomingInstances);
        }

        private void CalculateIncomingInstancesDiff(Dictionary<int, DynamicElympicsBehaviourInstanceData> instancesSerialized, Dictionary<int, DynamicElympicsBehaviourInstanceData> incomingInstancesSerialized)
        {
            _instancesToRemove.Clear();
            _instancesToAdd.Clear();

            foreach (var (instanceId, incomingInstanceData) in incomingInstancesSerialized)
            {
                if (instancesSerialized.TryGetValue(instanceId, out var currentInstanceData))
                {
                    if (currentInstanceData.Equals(incomingInstanceData))
                        continue;

                    _instancesToRemove.Add((instanceId, currentInstanceData));
                    _instancesToAdd.Add((instanceId, incomingInstanceData));
                }
                else
                {
                    _instancesToAdd.Add((instanceId, incomingInstanceData));
                }
            }

            foreach (var (instanceId, currentInstanceData) in instancesSerialized)
            {
                if (!incomingInstancesSerialized.ContainsKey(instanceId))
                    _instancesToRemove.Add((instanceId, currentInstanceData));
            }
        }

        internal bool AreIncomingInstancesTheSame() => _instancesToAdd.Count == 0 && _instancesToRemove.Count == 0;

        internal (IEnumerable<DynamicElympicsBehaviourInstanceData> instancesToRemove, IEnumerable<DynamicElympicsBehaviourInstanceData> instancesToAdd) GetIncomingDiff() =>
            (_instancesToRemove.Select(x => x.Item2), _instancesToAdd.Select(x => x.Item2));

        internal void ApplyIncomingInstances()
        {
            // Important - first remove obsolete instances as there could be same ids but other instance content, then add new
            foreach (var (instanceId, _) in _instancesToRemove)
                _ = _instances.Remove(instanceId);

            foreach (var (instanceId, instanceDataSerialized) in _instancesToAdd)
                _instances.Add(instanceId, instanceDataSerialized);
        }

        internal void Remove(int instanceId)
        {
            _ = _instances.Remove(instanceId);
        }

        internal int Add(int[] networkIds, string pathInResources)
        {
            var instanceId = _instancesCounter;
            _instancesCounter++;
            _instances.Add(instanceId, new DynamicElympicsBehaviourInstanceData(instanceId, networkIds, pathInResources));
            return instanceId;
        }

        internal int Count => _instances.Count;

        public bool Equals(FactoryPartState historyPartState, FactoryPartState receivedPartState, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var historyInstancesCount = historyPartState.DynamicInstancesState.InstancesCounter;
            var receivedInstancesCount = receivedPartState.DynamicInstancesState.InstancesCounter;

            _equalsInstancesHistory.Clear();
            foreach ((var key, var value) in historyPartState.DynamicInstancesState.Instances)
                _equalsInstancesHistory.Add(key, value);

            _equalsInstancesReceived.Clear();
            foreach ((var key, var value) in receivedPartState.DynamicInstancesState.Instances)
                _equalsInstancesReceived.Add(key, value);

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

            CalculateIncomingInstancesDiff(_equalsInstancesHistory, _equalsInstancesReceived);
            var areInstancesTheSame = AreIncomingInstancesTheSame();

#if !ELYMPICS_PRODUCTION
            if (!areInstancesTheSame)
            {
                var sb = new StringBuilder();
                _ = sb.Append("The dynamic object instances for player ").Append(player)
                    .Append(" in local snapshot history for tick ").Append(historyTick).Append(" don't match those received from the game server. ")
                    .Append("Last simulated tick: ").Append(lastSimulatedTick).Append(". ");

                if (_instancesToAdd.Count > 0)
                    _ = sb.Append($"Number of instances missing in local history: ").Append(_instancesToAdd.Count).Append(". ")
                        .Append($"Client either didn't predict that those instances should be spawned or incorrectly predicted that they should be destroyed. ");

                if (_instancesToRemove.Count > 0)
                    _ = sb.Append($"Number of instances that don't exist in the received state, but are present in local history: ")
                        .Append(_instancesToRemove.Count).Append(". ")
                        .Append($"Client either didn't predict that those instances should be destroyed or incorrectly predicted that they should be spawned.");

                ElympicsLogger.LogWarning(sb.ToString());
            }
#endif

            return areInstancesTheSame;
        }
    }
}
