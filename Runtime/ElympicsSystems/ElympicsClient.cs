using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public class ElympicsClient : ElympicsBase
	{
		private const int MaxInputsToSendOnPredictionJump       = 5;
		private const int MaxTicksToTickOnPredictionJump        = 3;

		[SerializeField] private bool connectOnStart = true;

		[SerializeField, Range(1, 60), Tooltip("In seconds")]
		private int networkConditionsLogInterval = 5;

		private         ElympicsPlayer _player;
		public override ElympicsPlayer Player   => _player;
		public override bool           IsClient => true;

		public  bool Initialized { get; private set; }
		private bool _started;

		public IMatchConnectClient MatchConnectClient
		{
			get
			{
				if (_matchConnectClient == null)
					throw new Exception("Elympics not initialized! Did you change ScriptExecutionOrder?");
				return _matchConnectClient;
			}
		}

		private IMatchConnectClient _matchConnectClient;
		private IMatchClient        _matchClient;

		// Prediction
		private IRoundTripTimeCalculator _roundTripTimeCalculator;
		private ClientTickCalculator     _clientTickCalculator;
		private PredictionBuffer         _predictionBuffer;

		private ElympicsSnapshot _lastReceivedSnapshot;
		private DateTime?        _lastClientPrintNetworkConditions;

		private List<ElympicsInput> _inputList;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, IMatchConnectClient matchConnectClient, IMatchClient matchClient, InitialMatchPlayerData initialMatchPlayerData)
		{
			base.InitializeInternal(elympicsGameConfig);
			_player = initialMatchPlayerData.Player;
			_matchConnectClient = matchConnectClient;
			_matchClient = matchClient;

			elympicsBehavioursManager.InitializeInternal(this);
			_roundTripTimeCalculator = new RoundTripTimeCalculator(_matchClient, _matchConnectClient);
			_clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator);
			_predictionBuffer = new PredictionBuffer(elympicsGameConfig);

			SetupCallbacks();
			OnStandaloneClientInit(initialMatchPlayerData);

			_inputList = new List<ElympicsInput>(1);

			Initialized = true;
			if (connectOnStart)
				StartCoroutine(ConnectAndJoinAsPlayer(_ => { }, default));
		}

		private void SetupCallbacks()
		{
			_matchClient.SnapshotReceived += OnSnapshotReceived;
			_matchClient.Synchronized += OnSynchronized;
			_matchConnectClient.DisconnectedByServer += OnDisconnectedByServer;
			_matchConnectClient.DisconnectedByClient += OnDisconnectedByClient;
			_matchConnectClient.ConnectedWithSynchronizationData += OnConnectedWithSynchronizationData;
			_matchConnectClient.ConnectingFailed += OnConnectingFailed;
			_matchConnectClient.AuthenticatedUserMatchWithUserId += OnAuthenticated;
			_matchConnectClient.AuthenticatedUserMatchFailedWithError += OnAuthenticatedFailed;
			_matchConnectClient.AuthenticatedAsSpectator += () => OnAuthenticated(null);
			_matchConnectClient.AuthenticatedAsSpectatorWithError += OnAuthenticatedFailed;
			_matchConnectClient.MatchJoinedWithMatchId += OnMatchJoined;
			_matchConnectClient.MatchJoinedWithError += OnMatchJoinedFailed;
			_matchConnectClient.MatchEndedWithMatchId += OnMatchEnded;
		}

		private void OnConnectedWithSynchronizationData(TimeSynchronizationData data)
		{
			_roundTripTimeCalculator.OnSynchronized(data);
			OnConnected(data);
		}

		private void OnDestroy()
		{
			if (_matchClient != null)
				_matchClient.SnapshotReceived -= OnSnapshotReceived;
		}

		private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
		{
			if (!_started) StartClient();

			if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
				_lastReceivedSnapshot = elympicsSnapshot;
		}

		private void StartClient() => _started = true;

		protected override bool ShouldDoFixedUpdate() => Initialized && _started;

		protected override void DoFixedUpdate()
		{
			var clientTickStart = DateTime.Now;
			if (Config.DetailedNetworkLog)
				LogNetworkConditionsInInterval(clientTickStart);

			var receivedSnapshot = _lastReceivedSnapshot;
			_clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick, receivedSnapshot.TickStartUtc, clientTickStart);
			_predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

			var lastDelayedInputTick = _clientTickCalculator.LastDelayedInputTick;
			var delayedInputTick = _clientTickCalculator.DelayedInputTick;

			var startDelayedInputTick = Math.Max(lastDelayedInputTick + 1, delayedInputTick - MaxInputsToSendOnPredictionJump);
			for (var i = startDelayedInputTick; i <= delayedInputTick; i++)
				ProcessInput(i);

			if (Config.Prediction)
			{
				ReconcileIfRequired(receivedSnapshot);

				var lastPredictionTick = _clientTickCalculator.LastPredictionTick;
				var predictionTick = _clientTickCalculator.PredictionTick;
				var tickDiff = predictionTick - (lastPredictionTick + 1);
				var startPredictionTick = tickDiff <= MaxTicksToTickOnPredictionJump ? lastPredictionTick + 1 : predictionTick;

				for (var i = startPredictionTick; i <= predictionTick; i++)
				{
					Tick = i;
					ApplyUnpredictablePartOfSnapshot(receivedSnapshot);
					elympicsBehavioursManager.CommitVars();
					ApplyPredictedInput();
					elympicsBehavioursManager.ElympicsUpdate();
					ProcessSnapshot(i);
				}
			}
			else
			{
				ApplyFullSnapshot(receivedSnapshot);
				elympicsBehavioursManager.CommitVars();
			}
		}
		private void LogNetworkConditionsInInterval(DateTime clientTickStart)
		{
			if (!_lastClientPrintNetworkConditions.HasValue)
				_lastClientPrintNetworkConditions = clientTickStart;

			if (!((clientTickStart - _lastClientPrintNetworkConditions.Value).TotalSeconds > networkConditionsLogInterval))
				return;

			_clientTickCalculator.LogNetworkConditions();
			_lastClientPrintNetworkConditions = clientTickStart;
		}

		private void ProcessSnapshot(long predictionTick)
		{
			var snapshot = elympicsBehavioursManager.GetLocalSnapshot();
			snapshot.Tick = predictionTick;
			_predictionBuffer.AddSnapshotToBuffer(snapshot);
		}

		private void ProcessInput(long delayedInputTick)
		{
			var input = elympicsBehavioursManager.OnInputForClient();
			AddMetadataToInput(input, delayedInputTick);
			SendInput(input);
			_predictionBuffer.AddInputToBuffer(input);
		}

		private void AddMetadataToInput(ElympicsInput input, long delayedInputTick)
		{
			input.Tick = delayedInputTick;
			input.Player = Player;
		}

		private void SendInput(ElympicsInput input) => _matchClient.SendInputUnreliable(input);

		private void ApplyPredictedInput()
		{
			_inputList.Clear();
			if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.PredictionTick, out var predictedInput))
				_inputList.Add(predictedInput);
			elympicsBehavioursManager.SetCurrentInputs(_inputList);
		}

		private void ApplyUnpredictablePartOfSnapshot(ElympicsSnapshot snapshot)
			=> elympicsBehavioursManager.ApplySnapshot(snapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

		private void ReconcileIfRequired(ElympicsSnapshot receivedSnapshot)
		{
			if (Config.ReconciliationFrequency == ElympicsGameConfig.ReconciliationFrequencyEnum.Never)
				return;

			if (!_predictionBuffer.TryGetSnapshotFromBuffer(receivedSnapshot.Tick, out var historySnapshot))
				return;

			if (elympicsBehavioursManager.AreSnapshotsEqualOnPredictableBehaviours(historySnapshot, receivedSnapshot) &&
			    Config.ReconciliationFrequency != ElympicsGameConfig.ReconciliationFrequencyEnum.OnEverySnapshot)
				return;

			Debug.LogWarning($"[Elympics] >>> Reconciliation on {receivedSnapshot.Tick} >>>");

			elympicsBehavioursManager.OnPreReconcile();

			// Debug.Log($"[{_player}] Applying snapshot {_lastReceivedSnapshot.Tick} with {JsonConvert.SerializeObject(_lastReceivedSnapshot, Formatting.Indented)}");
			Tick = receivedSnapshot.Tick;
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable, true);
			elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);
			elympicsBehavioursManager.CommitVars();

			var currentSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
			currentSnapshot.Tick = receivedSnapshot.Tick;
			_predictionBuffer.AddOrReplaceSnapshotInBuffer(currentSnapshot);

			for (var resimulationTick = receivedSnapshot.Tick + 1; resimulationTick < _clientTickCalculator.PredictionTick; resimulationTick++)
			{
				Tick = resimulationTick;
				if (_predictionBuffer.TryGetSnapshotFromBuffer(resimulationTick, out historySnapshot))
					elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);
				elympicsBehavioursManager.CommitVars();

				_inputList.Clear();
				if (_predictionBuffer.TryGetInputFromBuffer(resimulationTick, out var resimulatedInput))
					_inputList.Add(resimulatedInput);
				elympicsBehavioursManager.SetCurrentInputs(_inputList);
				elympicsBehavioursManager.ElympicsUpdate();

				var newResimulatedSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
				newResimulatedSnapshot.Tick = resimulationTick;
				_predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);
				// Debug.Log($"[{PlayerId}] Overriding snapshot {resimulationTick} with {JsonConvert.SerializeObject(newResimulatedSnapshot, Formatting.Indented)}");
			}

			elympicsBehavioursManager.OnPostReconcile();

			Debug.LogWarning($"[Elympics] <<< Reconciliation on {receivedSnapshot.Tick} finished <<<");
		}

		private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot) => elympicsBehavioursManager.ApplySnapshot(receivedSnapshot);

		#region IElympics

		public override IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)    => MatchConnectClient.ConnectAndJoinAsPlayer(connectedCallback, ct);
		public override IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectator(connectedCallback, ct);
		public override void        Disconnect()                                                                    => MatchConnectClient.Disconnect();

		#endregion
	}
}
