using System;
using System.Collections.Generic;
using System.IO;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	public class ElympicsBehavioursManager : MonoBehaviour
	{
		[SerializeField] private ElympicsBehavioursSerializableDictionary elympicsBehavioursView = new ElympicsBehavioursSerializableDictionary();
		[SerializeField] private ElympicsFactory                          factory;
		
		internal bool IsInElympicsUpdate { get; private set; }

		private          ElympicsBehavioursContainer _elympicsBehaviours;
		private readonly List<ElympicsBehaviour>     _bufferForIteration = new List<ElympicsBehaviour>();
		private          ElympicsBase                _elympics;
		private          BinaryInputWriter           _inputWriter;
		internal void InitializeInternal(ElympicsBase elympicsBase)
		{
			_inputWriter = new BinaryInputWriter();

			_elympics = elympicsBase;
			factory.Initialize(elympicsBase, AddNewBehaviour, RemoveBehaviour);

			_elympicsBehaviours = new ElympicsBehavioursContainer(_elympics.Player);
			var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
			foreach (var elympicsBehaviour in foundElympicsBehaviours)
			{
				var networkId = elympicsBehaviour.NetworkId;
				if (networkId < 0)
				{
					Debug.LogError($"Invalid networkId {networkId} on {elympicsBehaviour.gameObject.name} {elympicsBehaviour.GetType().Name}", elympicsBehaviour);
					return;
				}

				if (_elympicsBehaviours.Contains(networkId))
				{
					Debug.LogError($"Duplicated networkId {networkId} on {elympicsBehaviour.gameObject.name} {elympicsBehaviour.GetType().Name}", elympicsBehaviour);
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

		internal void AddNewBehaviour(ElympicsBehaviour elympicsBehaviour)
		{
			InitializeElympicsBehaviour(elympicsBehaviour);
			_elympicsBehaviours.Add(elympicsBehaviour);
		}

		internal void RemoveBehaviour(int networkId)
		{
			_elympicsBehaviours.Remove(networkId);
		}

		internal bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour)
		{
			return _elympicsBehaviours.Behaviours.TryGetValue(networkId, out elympicsBehaviour);
		}

		internal ElympicsInput OnInputForClient() => OnInput(ClientInputGetter);
		internal ElympicsInput OnInputForBot()    => OnInput(BotInputGetter);

		private static void ClientInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer) => behaviour.OnInputForClient(writer);
		private static void BotInputGetter(ElympicsBehaviour behaviour, BinaryInputWriter writer)    => behaviour.OnInputForBot(writer);

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
				foreach (var data in input.Data)
				{
					if (_elympicsBehaviours.BehavioursWithInput.TryGetValue(data.Key, out var elympicsBehaviour))
						elympicsBehaviour.SetCurrentInput(input.Player, data.Value);
				}
		}

		internal ElympicsSnapshot GetLocalSnapshot()
		{
			var snapshot = new ElympicsSnapshot
			{
				Factory = factory.GetState(),
				Data = new List<KeyValuePair<int, byte[]>>()
			};

			foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
			{
				if (!elympicsBehaviour.HasAnyState)
					continue;

				snapshot.Data.Add(new KeyValuePair<int, byte[]>(networkId, elympicsBehaviour.GetState()));
			}

			return snapshot;
		}

		internal Dictionary<ElympicsPlayer, ElympicsSnapshot> GetSnapshotsToSend(params ElympicsPlayer[] players)
		{
			var snapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();
			var factoryState = factory.GetState();
			foreach (var player in players)
				snapshots[player] = new ElympicsSnapshot
				{
					Factory = factoryState,
					Data = new List<KeyValuePair<int, byte[]>>()
				};

			foreach (var (networkId, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
			{
				if (!elympicsBehaviour.HasAnyState)
					continue;

				var state = elympicsBehaviour.GetState();

				if (!elympicsBehaviour.UpdateCurrentStateAndCheckIfSendCanBeSkipped(state))
				{
					var stateData = new KeyValuePair<int, byte[]>(networkId, state);

					foreach (var player in players)
					{
						if (elympicsBehaviour.IsVisibleTo(player))
							snapshots[player].Data.Add(stateData);
					}
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
			switch (statePredictability)
			{
				case StatePredictability.Predictable:
					return _elympicsBehaviours.BehavioursPredictable;
				case StatePredictability.Unpredictable:
					return _elympicsBehaviours.BehavioursUnpredictable;
				case StatePredictability.Both:
					return _elympicsBehaviours.Behaviours;
				default:
					throw new ArgumentOutOfRangeException(nameof(statePredictability), statePredictability, null);
			}
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
			if (!factory.ArePredictableStatesEqual(historySnapshot.Factory, receivedSnapshot.Factory))
			{
				Debug.LogWarning($"States not equal on factory");
				return false;
			}

			var chosenElympicsBehaviours = _elympicsBehaviours.BehavioursPredictable;

			// Todo optimize to not check whole snapshot, only predictable behaviours
			var historyIndex = 0;
			var receivedIndex = 0;
			while (historyIndex < historySnapshot.Data.Count && receivedIndex < receivedSnapshot.Data.Count)
			{
				var (historyNetworkId, historyState) = historySnapshot.Data[historyIndex];
				var (receivedNetworkId, receivedState) = receivedSnapshot.Data[receivedIndex];

				// Difference created by unpredictable factory
				if (historyNetworkId != receivedNetworkId)
				{
					if (historyNetworkId > receivedNetworkId)
					{
						receivedIndex++;
						continue;
					}

					if (historyNetworkId < receivedNetworkId)
					{
						historyIndex++;
						continue;
					}
				}

				historyIndex++;
				receivedIndex++;

				var networkId = historyNetworkId; // == receivedNetworkId

				// Behaviour should always exist - if there is difference in history and received snapshot then it will be omitted
				// It won't be found only if it's unpredictable
				if (!chosenElympicsBehaviours.TryGetValue(networkId, out var elympicsBehaviour))
					continue;

				if (elympicsBehaviour.AreStatesEqual(historyState, receivedState))
					continue;

				Debug.LogWarning($"States not equal on {networkId}");
				return false;
			}

			return true;
		}

		internal void ElympicsUpdate()
		{
			IsInElympicsUpdate = true;
			// copy behaviours to list before iterating because the collection might be modified by Instantiate/Destroy
			_bufferForIteration.Clear();
			_bufferForIteration.AddRange(_elympicsBehaviours.Behaviours.Values);
			foreach (var elympicsBehaviour in _bufferForIteration)
					elympicsBehaviour.ElympicsUpdate();
			IsInElympicsUpdate = false;
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

		internal void RefreshElympicsBehavioursView()
		{
			elympicsBehavioursView.Clear();
			var foundElympicsBehaviours = gameObject.FindObjectsOfTypeOnScene<ElympicsBehaviour>(true);
			foreach (var elympicsBehaviour in foundElympicsBehaviours)
			{
				var networkId = elympicsBehaviour.NetworkId;
				if (elympicsBehavioursView.ContainsKey(networkId))
				{
					Debug.LogWarning($"Cannot refresh behaviour with networkId {networkId}! Duplicated entry", elympicsBehaviour);
					continue;
				}

				elympicsBehavioursView.Add(networkId, elympicsBehaviour);
			}
		}

		#region ClientCallbacks

		internal void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnStandaloneClientInit(data);
		}

		internal void OnClientsOnServerInit(InitialMatchPlayerDatas data)
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

		internal void OnMatchJoined(string matchId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnMatchJoined(matchId);
		}

		internal void OnMatchEnded(string matchId)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnMatchEnded(matchId);
		}

		internal void OnAuthenticatedFailed(string errorMessage)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnAuthenticatedFailed(errorMessage);
		}

		internal void OnAuthenticated(string userId)
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

		internal void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnStandaloneBotInit(initialMatchData);
		}

		internal void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var (_, elympicsBehaviour) in _elympicsBehaviours.Behaviours)
				elympicsBehaviour.OnBotsOnServerInit(initialMatchData);
		}

		#endregion

		#region ServerCallbacks

		internal void OnServerInit(InitialMatchPlayerDatas initialMatchData)
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
