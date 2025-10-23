using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    public class ElympicsFactory : MonoBehaviour
    {
        private Action<ElympicsBehaviour> AddBehaviour;
        private Action<int> RemoveBehaviour;

        private ElympicsPlayer _player;
        private SortedDictionary<ElympicsPlayer, ElympicsFactoryPart> _elympicsFactoryParts;

        private FactoryNetworkIdEnumerator _checkEqualsEnumerator;
        private DynamicElympicsBehaviourInstancesData _checkEqualsData;

        private Dictionary<GameObject, ElympicsPlayer> _playersObjects;

        // Cache
        private HashSet<int> _predictablePlayers;
        private HashSet<int> _collectedPlayerIndexes;
        private List<ElympicsPlayer> _elympicsFactoryPartsToRemove;

        internal void Initialize(IElympics elympics, Action<ElympicsBehaviour> addBehaviour, Action<int> removeBehaviour)
        {
            _player = elympics.Player;

            AddBehaviour = addBehaviour;
            RemoveBehaviour = removeBehaviour;

            _elympicsFactoryParts = new SortedDictionary<ElympicsPlayer, ElympicsFactoryPart>();

            _checkEqualsEnumerator = new FactoryNetworkIdEnumerator(_player.StartNetworkId, _player.EndNetworkId);
            _checkEqualsData = new DynamicElympicsBehaviourInstancesData(_player.StartNetworkId);

            _playersObjects = new Dictionary<GameObject, ElympicsPlayer>();

            _predictablePlayers = new HashSet<int>
            {
                (int)_player,
                (int)ElympicsPlayer.All
            };
            _collectedPlayerIndexes = new HashSet<int>();
            _elympicsFactoryPartsToRemove = new List<ElympicsPlayer>();
        }

        internal GameObject CreateInstance(string pathInResources, ElympicsPlayer player, InstantiatedTransformConfig? transformConfig)
        {
            var elympicsFactoryPart = GetOrCreateFactoryPart(player);
            return elympicsFactoryPart.CreateInstance(pathInResources, transformConfig);
        }

        internal void DestroyInstance(GameObject go)
        {
            if (!_playersObjects.TryGetValue(go, out var player))
            {
                const string message = "Trying to destroy an object not instantiated by Elympics. "
                    + "This may also be a result of using an incorrectly cached reference after reconciliation or trying to destroy the same object more than once.";
                ElympicsLogger.LogError(message, go);
                throw new ArgumentException(message, nameof(go));
            }

            var elympicsFactoryPart = GetOrCreateFactoryPart(player);
            elympicsFactoryPart.DestroyInstance(go);

            if (elympicsFactoryPart.IsEmpty)
                RemoveFactoryPart(player);
        }

        internal FactoryState GetState()
        {
            var state = new FactoryState
            {
                Parts = _elympicsFactoryParts.Select(x => new KeyValuePair<int, byte[]>((int)x.Key, x.Value.GetState())).ToList()
            };

            return state;
        }

        internal void ApplyState(FactoryState state, HashSet<int> playerIndexesIncludeOnly = null, HashSet<int> playerIndexesToExclude = null)
        {
            var onlyInclude = playerIndexesIncludeOnly != null;
            var exclude = playerIndexesToExclude != null;
            if (onlyInclude && exclude)
                throw new ArgumentException("Cannot include and exclude at the same time");

            _collectedPlayerIndexes.Clear();
            foreach (var (playerIndex, data) in state.Parts)
            {
                if (onlyInclude)
                {
                    if (!playerIndexesIncludeOnly.Contains(playerIndex))
                        continue;
                }
                else if (exclude)
                {
                    if (playerIndexesToExclude.Contains(playerIndex))
                        continue;
                }

                var player = ElympicsPlayer.FromIndexExtended(playerIndex);
                _ = _collectedPlayerIndexes.Add(playerIndex);

                var elympicsFactoryPart = GetOrCreateFactoryPart(player);
                elympicsFactoryPart.ApplyState(data);
            }

            if (onlyInclude)
            {
                _collectedPlayerIndexes.SymmetricExceptWith(playerIndexesIncludeOnly);
                foreach (var playerIndexToRemove in _collectedPlayerIndexes)
                    RemoveFactoryPart(ElympicsPlayer.FromIndexExtended(playerIndexToRemove));

                return;
            }

            if (exclude)
                _collectedPlayerIndexes.UnionWith(playerIndexesToExclude);

            RemoveIfNotInState(_collectedPlayerIndexes);
        }

        internal void ApplyPredictableState(FactoryState state) => ApplyState(state, _predictablePlayers, null);
        internal void ApplyUnpredictableState(FactoryState state) => ApplyState(state, null, _predictablePlayers);

        private void RemoveIfNotInState(HashSet<int> playerIndexes)
        {
            _elympicsFactoryPartsToRemove.Clear();
            foreach (var player in _elympicsFactoryParts.Keys)
            {
                if (!playerIndexes.Contains((int)player))
                    _elympicsFactoryPartsToRemove.Add(player);
            }

            foreach (var playerIndex in _elympicsFactoryPartsToRemove)
                RemoveFactoryPart(playerIndex);
        }

        private ElympicsFactoryPart GetOrCreateFactoryPart(ElympicsPlayer player)
        {
            if (_elympicsFactoryParts.TryGetValue(player, out var elympicsFactoryPart))
                return elympicsFactoryPart;

            elympicsFactoryPart = new ElympicsFactoryPart(player, Instantiate, Destroy, AddBehaviour, RemoveBehaviour);
            _elympicsFactoryParts.Add(player, elympicsFactoryPart);

            return elympicsFactoryPart;
        }

        private void RemoveFactoryPart(ElympicsPlayer player)
        {
            if (_elympicsFactoryParts.TryGetValue(player, out var elympicsFactoryPart))
                elympicsFactoryPart.DestroyAllInstances();

            _ = _elympicsFactoryParts.Remove(player);
        }

        private GameObject Instantiate(ElympicsPlayer player, GameObject prefabGameObject, InstantiatedTransformConfig? transformConfig)
        {
            var instantiatedGameObject = transformConfig.HasValue ? Instantiate(prefabGameObject, transformConfig.Value.Position, transformConfig.Value.Rotation) : Instantiate(prefabGameObject);
            _playersObjects.Add(instantiatedGameObject, player);
            return instantiatedGameObject;
        }

        private void Destroy(ElympicsPlayer player, GameObject go)
        {
            _ = _playersObjects.Remove(go);
            Destroy(go);
        }

        internal bool ArePredictableStatesEqual(FactoryState historyState, FactoryState receivedState)
        {
            return ArePredictableStatesEqualForPlayer(historyState, receivedState, _player) && ArePredictableStatesEqualForPlayer(historyState, receivedState, ElympicsPlayer.All);
        }

        private bool ArePredictableStatesEqualForPlayer(FactoryState historyState, FactoryState receivedState, ElympicsPlayer player)
        {
            var playerIndex = (int)player;
            var historyStateData = FindStateData(historyState, playerIndex);
            var receivedStateData = FindStateData(receivedState, playerIndex);

            var historyStateDataExists = historyStateData != null;
            var receivedStateDataExists = receivedStateData != null;

            if (receivedStateDataExists && !historyStateDataExists)
                return false;
            if (!receivedStateDataExists && historyStateDataExists)
                return false;
            if (!receivedStateDataExists && !historyStateDataExists)
                return true;

            using var ms1 = new MemoryStream(historyStateData.Value.Value);
            using var br1 = new BinaryReader(ms1);
            using var ms2 = new MemoryStream(receivedStateData.Value.Value);
            using var br2 = new BinaryReader(ms2);

            return _checkEqualsEnumerator.Equals(br1, br2) && _checkEqualsData.Equals(br1, br2);
        }

        private static KeyValuePair<int, byte[]>? FindStateData(FactoryState state, int playerIndex)
        {
            KeyValuePair<int, byte[]>? stateData = null;
            foreach (var partData in state.Parts)
            {
                if (partData.Key != playerIndex)
                    continue;

                stateData = partData;
                break;
            }

            return stateData;
        }
    }
}
