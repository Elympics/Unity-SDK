using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Behaviour;
using Elympics.Communication.Models.Public;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    public class ElympicsBehavioursManager : MonoBehaviour
    {
        [SerializeField] internal ElympicsBehavioursSerializableDictionary elympicsBehavioursView = new();
        [SerializeField] internal ElympicsFactory factory;

        private ElympicsBehavioursContainer _elympicsBehaviours;

        private readonly List<ElympicsBehaviour> _bufferForIteration = new();
        private ElympicsBase _elympics;
        private BinaryInputWriter _inputWriter;

        internal const int NetworkIdRange = 10000000;
        internal void InitializeInternal(ElympicsBase elympicsBase)
        {
            _inputWriter = new BinaryInputWriter();

            _elympics = elympicsBase;
            factory.Initialize(elympicsBase, AddNewBehaviour, RemoveBehaviour);

            _elympicsBehaviours = new ElympicsBehavioursContainer(_elympics.Player);
            var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
            foundElympicsBehaviours.Sort((a, b) => Comparer<int>.Default.Compare(a.networkId, b.networkId));
            foreach (var elympicsBehaviour in foundElympicsBehaviours)
            {
                var networkId = elympicsBehaviour.NetworkId;
                if (networkId < 0) // there is no upper limit
                {
                    ElympicsLogger.LogError($"Invalid network ID {networkId} for {elympicsBehaviour.gameObject.name} object. "
                        + "Network ID must not be negative.",
                        elympicsBehaviour);
                    return;
                }

                if (_elympicsBehaviours.Contains(networkId))
                {
                    ElympicsLogger.LogError($"Duplicated network ID {networkId} detected on {elympicsBehaviour.gameObject.name} object.\n"
                        + $"Previous occurrence: {_elympicsBehaviours.Behaviours[networkId].gameObject.name} object",
                        elympicsBehaviour);
                    return;
                }

                InitializeElympicsBehaviour(elympicsBehaviour);
                _elympicsBehaviours.Add(elympicsBehaviour);
            }
        }

        private void OnDestroy()
        {
            _inputWriter?.Dispose();
        }

        private void InitializeElympicsBehaviour(ElympicsBehaviour elympicsBehaviour)
        {
            elympicsBehaviour.InitializeInternal(_elympics);
        }

        private void AddNewBehaviour(ElympicsBehaviour elympicsBehaviour)
        {
            InitializeElympicsBehaviour(elympicsBehaviour);
            _elympicsBehaviours.Add(elympicsBehaviour);
        }

        private void RemoveBehaviour(int networkId)
        {
            _elympicsBehaviours.Remove(networkId);
        }

        internal List<GameObject> GetAllElympicsBehavioursGO()
        {
            return _elympicsBehaviours.Behaviours.Values.Select(x => x.gameObject).ToList();
        }

        internal bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour)
        {
            return _elympicsBehaviours.Behaviours.TryGetValue(networkId, out elympicsBehaviour);
        }

        internal ElympicsInput OnInputForClient() => OnInput(ClientInputGetter);
        internal ElympicsInput OnInputForBot() => OnInput(BotInputGetter);

        private static void ClientInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer) => behaviour.OnInputForClient(writer);
        private static void BotInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer) => behaviour.OnInputForBot(writer);

        private ElympicsInput OnInput(Action<ElympicsBehaviour, BinaryInputWriter> onInput)
        {
            var input = new ElympicsInput
            {
                Data = new List<KeyValuePair<int, byte[]>>()
            };

            foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.BehavioursWithInput)
            {
                if (!elympicsBehaviour.HasAnyInput)
                    continue;

                onInput(elympicsBehaviour, _inputWriter);

                var serializedData = _inputWriter.GetData();
                if (serializedData != null && serializedData.Length != 0)
                    input.Data.Add(new KeyValuePair<int, byte[]>(networkId, serializedData));
                _inputWriter.ResetStream();
            }

            return input;
        }

        internal void SetCurrentInputs(List<ElympicsInput> inputs)
        {
            foreach (var behaviour in _elympicsBehaviours.BehavioursWithInput.Values)
                behaviour.ClearInputs();

            foreach (var input in inputs)
                foreach (var (networkId, inputBuffer) in input.Data)
                {
                    if (_elympicsBehaviours.BehavioursWithInput.TryGetValue(networkId, out var elympicsBehaviour))
                        elympicsBehaviour.SetCurrentInput(input.Player, input.Tick, inputBuffer);
                }
        }

        internal ElympicsSnapshot GetLocalSnapshot()
        {
            var snapshot = new ElympicsSnapshot(factory.GetState(), new Dictionary<int, byte[]>(_elympicsBehaviours.Behaviours.Count))
            {
                TickStartUtc = _elympics.TickStartUtc
            };

            //Behaviours should always be added to snapshot in that order, so they remain ordered by ID and other code can use that for optimization
            foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
            {
                if (!elympicsBehaviour.HasAnyState)
                    continue;

                snapshot.Data.Add(networkId, elympicsBehaviour.GetState());
            }

            return snapshot;
        }

        public ElympicsSnapshotWithMetadata AddMetadataToSnapshot(ElympicsSnapshot snapshot)
        {
            var snapshotWithMetadata = new ElympicsSnapshotWithMetadata(snapshot, _elympics.TickEndUtc);

            foreach (var (id, _) in snapshot.Data)
            {
                var elympicsBehaviour = _elympicsBehaviours.Behaviours[id];

                snapshotWithMetadata.Metadata.Add(new ElympicsBehaviourMetadata
                {
                    Name = elympicsBehaviour.name,
                    NetworkId = elympicsBehaviour.NetworkId,
                    PredictableFor = elympicsBehaviour.PredictableFor,
                    PrefabName = elympicsBehaviour.PrefabName,
                });
            }

            return snapshotWithMetadata;
        }

        public void AddStateMetaData(ElympicsSnapshotWithMetadata snapshotWithMetadata)
        {
            foreach (var (id, _) in snapshotWithMetadata.Data)
            {
                var elympicsBehaviour = _elympicsBehaviours.Behaviours[id];
                var index = snapshotWithMetadata.Metadata.FindIndex(x => x.NetworkId == id);
                if (index == -1)
                    throw new Exception("No metadata found for id: " + id);

                var oldMetadata = snapshotWithMetadata.Metadata[index];
                var stateMetaData = elympicsBehaviour.GetStateMetadata();
                var newMetaData = oldMetadata.WithStateMetadata(stateMetaData);
                snapshotWithMetadata.Metadata[index] = newMetaData;
            }
        }

        internal Dictionary<ElympicsPlayer, ElympicsSnapshot> GetSnapshotsToSend(ElympicsSnapshot fullSnapshot, params PlayerData[] playerDatas)
        {
            var snapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>(playerDatas.Length);

            foreach (var playerData in playerDatas)
            {
                var snapshot = ElympicsSnapshot.CreateDeepCopy(fullSnapshot);
                _ = snapshot.TickToPlayersInputData.Remove((int)playerData.Player);
                snapshots[playerData.Player] = snapshot;
            }

            //Behaviours should always be added to snapshot in that order, so they remain ordered by ID and other code can use that for optimization
            foreach (var (id, state) in fullSnapshot.Data)
            {
                var elympicsBehaviour = _elympicsBehaviours.Behaviours[id];
                var canBeSkipped = elympicsBehaviour.UpdateCurrentStateAndCheckIfSendCanBeSkipped(state, fullSnapshot.Tick);

                foreach (var playerData in playerDatas)
                {
                    //If player didn't receive any snapshots, don't skip any visible behaviours
                    if (playerData.LastReceivedSnapshot >= 0 && canBeSkipped)
                        continue;
                    if (!elympicsBehaviour.IsVisibleTo(playerData.Player))
                        continue;

                    snapshots[playerData.Player].Data.Add(id, state);
                }
            }

            return snapshots;
        }

        internal void ApplySnapshot(ElympicsSnapshot elympicsSnapshot, StatePredictability statePredictability = StatePredictability.Both, bool ignoreTolerance = false)
        {
            ApplyFactoryBasedOnStatePredictability(elympicsSnapshot.Factory, statePredictability);

            var chosenElympicsBehaviours = GetElympicsBehavioursBasedOnStatePredictability(statePredictability);
            foreach (var data in elympicsSnapshot.Data)
            {
                if (chosenElympicsBehaviours.TryGetValue(data.Key, out var elympicsBehaviour))
                    elympicsBehaviour.ApplyState(data.Value, ignoreTolerance);
            }
        }

        private IReadOnlyDictionary<int, ElympicsBehaviour> GetElympicsBehavioursBasedOnStatePredictability(StatePredictability statePredictability)
        {
            return statePredictability switch
            {
                StatePredictability.Predictable => _elympicsBehaviours.BehavioursPredictable,
                StatePredictability.Unpredictable => _elympicsBehaviours.BehavioursUnpredictable,
                StatePredictability.Both => _elympicsBehaviours.Behaviours,
                _ => throw new ArgumentOutOfRangeException(nameof(statePredictability), statePredictability, null),
            };
        }

        private void ApplyFactoryBasedOnStatePredictability(FactoryState state, StatePredictability statePredictability)
        {
            switch (statePredictability)
            {
                case StatePredictability.Predictable:
                    factory.ApplyPredictableState(state);
                    break;
                case StatePredictability.Unpredictable:
                    factory.ApplyUnpredictableState(state);
                    break;
                case StatePredictability.Both:
                    factory.ApplyState(state);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statePredictability), statePredictability, null);
            }
        }

        internal bool AreSnapshotsEqualOnPredictableBehaviours(ElympicsSnapshot historySnapshot, ElympicsSnapshot receivedSnapshot)
        {
            var historyTick = receivedSnapshot.Tick;
            var lastSimulatedTick = _elympics.Tick;

            if (!factory.ArePredictableStatesEqual(historySnapshot.Factory, receivedSnapshot.Factory, historyTick, lastSimulatedTick))
                return false;

            var chosenElympicsBehaviours = _elympicsBehaviours.BehavioursPredictable;
            var finder = new NetworkBehaviourFinder(historySnapshot, receivedSnapshot);
            // Todo optimize to not check whole snapshot, only predictable behaviours

            foreach (var behaviourPair in finder)
            {
                // Behaviour should always exist - if there is difference in history and received snapshot then it will be omitted
                // It won't be found only if it's unpredictable
                if (!chosenElympicsBehaviours.TryGetValue(behaviourPair.NetworkId, out var elympicsBehaviour))
                    continue;

                if (elympicsBehaviour.AreStatesEqual(behaviourPair.DataFromFirst, behaviourPair.DataFromSecond, historyTick))
                    continue;

                return false;
            }
            return true;
        }

        internal void CommitVars()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.CommitVars();
        }

        internal void ElympicsUpdate()
        {
            // copy behaviours to list before iterating because the collection might be modified by Instantiate/Destroy
            _bufferForIteration.Clear();
            _bufferForIteration.AddRange(_elympicsBehaviours.Behaviours.Values);
            foreach (var elympicsBehaviour in _bufferForIteration)
                elympicsBehaviour.ElympicsUpdate();
        }

        internal void Render(in RenderData renderData)
        {
            foreach (var elympicsBehaviour in _elympicsBehaviours.Behaviours.Values)
                elympicsBehaviour.OnRender(renderData);
        }

        internal void OnPreReconcile()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnPreReconcile();
        }

        internal void OnPostReconcile()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnPostReconcile();
        }

        internal void OnPredictionStatusChanged(bool isBlocked, ClientTickCalculatorNetworkDetails results)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnPredictionStatsChanged(isBlocked, results);
        }

        internal void RefreshElympicsBehavioursView()
        {
            elympicsBehavioursView.Clear();
            var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
            foreach (var elympicsBehaviour in foundElympicsBehaviours)
            {
                var networkId = elympicsBehaviour.NetworkId;
                if (elympicsBehavioursView.ContainsKey(networkId))
                {
                    ElympicsLogger.LogWarning($"Duplicated entry detected for network ID {networkId}!", elympicsBehaviour);
                    continue;
                }

                elympicsBehavioursView.Add(networkId, elympicsBehaviour);
            }
        }

        #region ClientCallbacks

        internal void OnStandaloneClientInit(InitialMatchPlayerDataGuid data)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnStandaloneClientInit(data);
        }

        internal void OnClientsOnServerInit(InitialMatchPlayerDatasGuid data)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnClientsOnServerInit(data);
        }

        internal void OnSynchronized(TimeSynchronizationData data)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnSynchronized(data);
        }

        internal void OnMatchJoinedFailed(string errorMessage)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnMatchJoinedFailed(errorMessage);
        }

        public void OnMatchJoinedWithInitData(MatchInitialData matchInitData)
        {
            #region ObsoleteCalls
            // Obsolete - to be removed in future versions. Leave for backward compatibility.
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnMatchJoined(matchInitData.MatchId);
            #endregion

            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnMatchJoinedWithInitData(matchInitData);
        }
        internal void OnMatchEnded(Guid matchId)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnMatchEnded(matchId);
        }

        internal void OnAuthenticatedFailed(string errorMessage)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnAuthenticatedFailed(errorMessage);
        }

        internal void OnAuthenticated(Guid userId)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnAuthenticated(userId);
        }

        internal void OnDisconnectedByServer()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnDisconnectedByServer();
        }

        internal void OnDisconnectedByClient()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnDisconnectedByClient();
        }

        internal void OnConnected(TimeSynchronizationData data)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnConnected(data);
        }

        internal void OnConnectingFailed()
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnConnectingFailed();
        }

        #endregion

        #region BotCallbacks

        internal void OnStandaloneBotInit(InitialMatchPlayerDataGuid initialMatchData)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnStandaloneBotInit(initialMatchData);
        }

        internal void OnBotsOnServerInit(InitialMatchPlayerDatasGuid initialMatchData)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnBotsOnServerInit(initialMatchData);
        }

        #endregion

        #region ServerCallbacks

        internal void OnServerInit(InitialMatchPlayerDatasGuid initialMatchData)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnServerInit(initialMatchData);
        }

        internal void OnPlayerConnected(ElympicsPlayer player)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnPlayerConnected(player);
        }

        internal void OnPlayerDisconnected(ElympicsPlayer player)
        {
            foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
                elympicsBehaviour.OnPlayerDisconnected(player);
        }

        #endregion

        internal enum StatePredictability
        {
            Predictable,
            Unpredictable,
            Both
        }
    }
}
