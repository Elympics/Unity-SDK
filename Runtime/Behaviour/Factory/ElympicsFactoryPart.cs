using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elympics
{
    internal class ElympicsFactoryPart
    {
        private readonly Func<ElympicsPlayer, GameObject, InstantiatedTransformConfig?, GameObject> _instantiate;
        private readonly Action<ElympicsPlayer, GameObject> _destroy;
        private readonly Action<ElympicsBehaviour> _addBehaviour;
        private readonly Action<int> _removeBehaviour;

        private readonly ElympicsPlayer _player;

        private readonly FactoryNetworkIdEnumerator _currentNetworkId;
        private readonly DynamicElympicsBehaviourInstancesData _instancesData;

        private readonly Dictionary<int, CreatedInstanceWrapper> _createdInstanceWrappersCache;
        private readonly Dictionary<GameObject, int> _createdGameObjectsIds;

        internal ElympicsFactoryPart(
            ElympicsPlayer player,
            Func<ElympicsPlayer, GameObject, InstantiatedTransformConfig?, GameObject> instantiate,
            Action<ElympicsPlayer, GameObject> destroy,
            Action<ElympicsBehaviour> addBehaviour,
            Action<int> removeBehaviour)
        {
            _player = player;

            _currentNetworkId = new FactoryNetworkIdEnumerator((int)player);
            _instancesData = new DynamicElympicsBehaviourInstancesData(_currentNetworkId.GetCurrent());
            _createdInstanceWrappersCache = new Dictionary<int, CreatedInstanceWrapper>();
            _createdGameObjectsIds = new Dictionary<GameObject, int>();
            _instantiate = instantiate;
            _destroy = destroy;
            _addBehaviour = addBehaviour;
            _removeBehaviour = removeBehaviour;
        }

        internal bool IsEmpty => _instancesData.Count == 0;

        internal FactoryPartState GetState() => new(_currentNetworkId.GetCurrent(), _instancesData.GetState());

        internal void ApplyState(FactoryPartState data)
        {
            _currentNetworkId.MoveTo(data.CurrentNetworkId);
            _instancesData.ApplyState(data.DynamicInstancesState);

            OnPostStateApplied();
        }

        private void OnPostStateApplied()
        {
            if (_instancesData.AreIncomingInstancesTheSame())
                return;

            var (instancesToRemove, instancesToAdd) = _instancesData.GetIncomingDiff();

            foreach (var instanceData in instancesToRemove)
                DestroyInstanceInternal(instanceData.ID);

            foreach (var instanceData in instancesToAdd)
                _ = CreateInstanceInternal(instanceData.ID, instanceData.InstanceType, null, instanceData.NetworkIds);

            _instancesData.ApplyIncomingInstances();
        }

        internal GameObject CreateInstance(string pathInResources, InstantiatedTransformConfig? transformConfig)
        {
            var instanceWrapper = CreateInstanceInternal(null, pathInResources, transformConfig, null);
            // Record the instance now that we know its NetworkIds
            var instanceId = _instancesData.Add(instanceWrapper.NetworkIds, pathInResources);
            _createdInstanceWrappersCache[instanceId] = instanceWrapper;
            _createdGameObjectsIds[instanceWrapper.GameObject] = instanceId;
            return instanceWrapper.GameObject;
        }

        /// <summary>
        /// Creates an instance of the prefab at <paramref name="pathInResources"/>.
        /// </summary>
        /// <param name="instanceId">
        /// When non-null the caller has already reserved the instanceId (client apply path).
        /// When null this method does NOT add to dictionaries — the caller does it after.
        /// </param>
        /// <param name="pathInResources">Path passed to <see cref="Resources.Load{T}"/>.</param>
        /// <param name="transformConfig">Optional spawn transform.</param>
        /// <param name="explicitNetworkIds">
        /// When non-null (client path), assign these NetworkIds directly to each behaviour in order,
        /// and sync them with the enumerator so future server allocations do not collide.
        /// When null (server path), let the enumerator allocate new ids via <see cref="FactoryNetworkIdEnumerator.MoveNextAndGetCurrent"/>.
        /// </param>
        private CreatedInstanceWrapper CreateInstanceInternal(int? instanceId, string pathInResources, InstantiatedTransformConfig? transformConfig, int[] explicitNetworkIds)
        {
            var createdPrefab = Resources.Load<GameObject>(pathInResources)
                                ?? throw new ArgumentException($"Prefab you want to instantiate ({pathInResources}) does not exist");
            var createdGameObject = _instantiate(_player, createdPrefab, transformConfig);
            var elympicsBehaviours = createdGameObject.GetComponentsInChildren<ElympicsBehaviour>(true);

            if (explicitNetworkIds != null && explicitNetworkIds.Length != elympicsBehaviours.Length)
                throw new InvalidOperationException(
                    $"NetworkIds array length ({explicitNetworkIds.Length}) does not match the number of ElympicsBehaviours ({elympicsBehaviours.Length}) on prefab '{pathInResources}'. "
                    + "This indicates a prefab mismatch between server and client.");

            var assignedNetworkIds = new int[elympicsBehaviours.Length];

            for (var i = 0; i < elympicsBehaviours.Length; i++)
            {
                var behaviour = elympicsBehaviours[i];
                int networkId;

                if (explicitNetworkIds != null)
                {
                    // Client path: assign the server-authoritative NetworkId directly
                    networkId = explicitNetworkIds[i];
                    behaviour.NetworkId = networkId;
                    // Keep enumerator state consistent so future allocations don't collide
                    _currentNetworkId.SyncAllocatedId(networkId);
                }
                else
                {
                    // Server path: let the enumerator generate the next id
                    networkId = _currentNetworkId.MoveNextAndGetCurrent();
                    behaviour.NetworkId = networkId;
                }

                assignedNetworkIds[i] = networkId;
                behaviour.PredictableFor = _player;
                behaviour.PrefabName = pathInResources;
                _addBehaviour?.Invoke(behaviour);
            }

            var instanceWrapper = new CreatedInstanceWrapper
            {
                GameObject = createdGameObject,
                NetworkIds = assignedNetworkIds,
            };

            // When instanceId is provided (client apply path), register in caches immediately
            if (instanceId.HasValue)
            {
                _createdInstanceWrappersCache.Add(instanceId.Value, instanceWrapper);
                _createdGameObjectsIds.Add(instanceWrapper.GameObject, instanceId.Value);
            }

            return instanceWrapper;
        }

        internal void DestroyInstance(GameObject createGameObject)
        {
            if (!_createdGameObjectsIds.TryGetValue(createGameObject, out var instanceId))
                throw new ArgumentException("Trying to destroy object not created by ElympicsFactory", nameof(createGameObject));

            DestroyInstanceInternal(instanceId);
        }

        private void DestroyInstanceInternal(int instanceId)
        {
            if (!_createdInstanceWrappersCache.TryGetValue(instanceId, out var instance))
                throw new ArgumentException($"Fatal error! Created game object with id {instanceId}, doesn't have cached instance", nameof(instanceId));

            foreach (var instanceNetworkId in instance.NetworkIds)
            {
                _removeBehaviour?.Invoke(instanceNetworkId);
                _currentNetworkId.ReleaseId(instanceNetworkId);
            }

            _instancesData.Remove(instanceId);
            _ = _createdGameObjectsIds.Remove(instance.GameObject);
            _ = _createdInstanceWrappersCache.Remove(instanceId);
            _destroy(_player, instance.GameObject);
        }

        internal void DestroyAllInstances()
        {
            var instancesIds = new List<int>();
            instancesIds.AddRange(_createdGameObjectsIds.Values);
            foreach (var instanceId in instancesIds)
                DestroyInstanceInternal(instanceId);
        }

        private class CreatedInstanceWrapper
        {
            public GameObject GameObject { get; set; }
            public int[] NetworkIds { get; set; }
        }
    }
}
