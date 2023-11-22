using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    internal class ElympicsFactoryPart
    {
        private readonly Func<ElympicsPlayer, GameObject, GameObject> _instantiate;
        private readonly Action<ElympicsPlayer, GameObject> _destroy;
        private readonly Action<ElympicsBehaviour> _addBehaviour;
        private readonly Action<int> _removeBehaviour;

        private readonly ElympicsPlayer _player;

        private readonly FactoryNetworkIdEnumerator _currentNetworkId;
        private readonly DynamicElympicsBehaviourInstancesData _instancesData;

        private readonly Dictionary<int, CreatedInstanceWrapper> _createdInstanceWrappersCache;
        private readonly Dictionary<GameObject, int> _createdGameObjectsIds;

        internal ElympicsFactoryPart(ElympicsPlayer player, Func<ElympicsPlayer, GameObject, GameObject> instantiate, Action<ElympicsPlayer, GameObject> destroy, Action<ElympicsBehaviour> addBehaviour, Action<int> removeBehaviour)
        {
            _player = player;

            _currentNetworkId = new FactoryNetworkIdEnumerator(player.StartNetworkId, player.EndNetworkId);
            _instancesData = new DynamicElympicsBehaviourInstancesData(player.StartNetworkId);
            _createdInstanceWrappersCache = new Dictionary<int, CreatedInstanceWrapper>();
            _createdGameObjectsIds = new Dictionary<GameObject, int>();
            _instantiate = instantiate;
            _destroy = destroy;
            _addBehaviour = addBehaviour;
            _removeBehaviour = removeBehaviour;
        }

        internal bool IsEmpty => _instancesData.Count == 0;

        internal byte[] GetState()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            _currentNetworkId.Serialize(bw);
            _instancesData.Serialize(bw);
            return ms.ToArray();
        }

        internal void ApplyState(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                _currentNetworkId.Deserialize(br);
                _instancesData.Deserialize(br);
            }

            OnPostDataDeserialize();
        }

        private void OnPostDataDeserialize()
        {
            var cachedNetworkIdEnumerator = _currentNetworkId.GetCurrent();
            if (!_instancesData.AreIncomingInstancesTheSame())
            {
                var (instancesToRemove, instancesToAdd) = _instancesData.GetIncomingDiff();
                foreach (var instanceData in instancesToRemove)
                    DestroyInstanceInternal(instanceData.ID);

                foreach (var instanceData in instancesToAdd)
                {
                    _currentNetworkId.MoveTo(instanceData.PrecedingNetworkIdEnumeratorValue);
                    _ = CreateInstanceInternal(instanceData.ID, instanceData.InstanceType);
                }

                _instancesData.ApplyIncomingInstances();
            }

            _currentNetworkId.MoveTo(cachedNetworkIdEnumerator);
        }

        internal GameObject CreateInstance(string pathInResources)
        {
            var instanceId = _instancesData.Add(_currentNetworkId.GetCurrent(), pathInResources);
            var instanceWrapper = CreateInstanceInternal(instanceId, pathInResources);
            return instanceWrapper.GameObject;
        }

        private CreatedInstanceWrapper CreateInstanceInternal(int instanceId, string pathInResources)
        {
            var createdPrefab = Resources.Load<GameObject>(pathInResources)
                                ?? throw new ArgumentException($"Prefab you want to instantiate ({pathInResources}) does not exist");
            var createdGameObject = _instantiate(_player, createdPrefab);
            var elympicsBehaviours = createdGameObject.GetComponentsInChildren<ElympicsBehaviour>(true);
            foreach (var behaviour in elympicsBehaviours)
            {
                behaviour.NetworkId = _currentNetworkId.MoveNextAndGetCurrent();
                behaviour.PredictableFor = _player;
                behaviour.PrefabName = pathInResources;
                _addBehaviour?.Invoke(behaviour);
            }

            var instanceWrapper = new CreatedInstanceWrapper
            {
                GameObject = createdGameObject,
                NetworkIds = elympicsBehaviours.Select(x => x.NetworkId).ToList()
            };

            _createdInstanceWrappersCache.Add(instanceId, instanceWrapper);
            _createdGameObjectsIds.Add(instanceWrapper.GameObject, instanceId);

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
            public List<int> NetworkIds { get; set; }
        }
    }
}
