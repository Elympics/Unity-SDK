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
		[SerializeField] private bool connectOnStart = true;

		[SerializeField, Range(0, 60), Tooltip("In seconds")]
		private int networkConditionsLogInterval = 5;

		private         ElympicsPlayer _player;
		public override ElympicsPlayer Player   => _player;
		public override bool           IsClient => true;

		private bool _started;
		private ClientTickCalculatorNetworkDetailsToFile _logToFile;
		public IMatchConnectClient MatchConnectClient
		{
			get
			{
				if (_matchConnectClient == null)
					throw new ElympicsException("Elympics not initialized! Did you change ScriptExecutionOrder?");
				return _matchConnectClient;
			}
		}


		private IMatchConnectClient _matchConnectClient;
		private IMatchClient        _matchClient;

		// Prediction
		private IRoundTripTimeCalculator _roundTripTimeCalculator;
		private ClientTickCalculator     _clientTickCalculator;
		private PredictionBuffer         _predictionBuffer;

		private static readonly object           LastReceivedSnapshotLock = new object();
		private                 ElympicsSnapshot _lastReceivedSnapshot;

		private DateTime? _lastClientPrintNetworkConditions;
		private long      _lastPredictedTick;
		private long      _lastDelayedInputTick;

		private List<ElympicsInput> _inputList;

		protected override double MaxUpdateTimeWarningThreshold => 1 / Config.MaxTickRate;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, IMatchConnectClient matchConnectClient, IMatchClient matchClient, InitialMatchPlayerDataGuid initialMatchPlayerData)
		{
			base.InitializeInternal(elympicsGameConfig);
			_player = initialMatchPlayerData.Player;
			_matchConnectClient = matchConnectClient;
			_matchClient = matchClient;

			elympicsBehavioursManager.InitializeInternal(this);
			_roundTripTimeCalculator = new RoundTripTimeCalculator(_matchClient, _matchConnectClient);
#if ELYMPICS_DEBUG
			_logToFile = new ClientTickCalculatorNetworkDetailsToFile();
#endif
			_clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator, elympicsGameConfig);
			_predictionBuffer = new PredictionBuffer(elympicsGameConfig);

			SetupCallbacks();
			OnStandaloneClientInit(initialMatchPlayerData);

			_inputList = new List<ElympicsInput>(1);

			SetInitialized();

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
			_matchConnectClient.AuthenticatedAsSpectator += () => OnAuthenticated(Guid.Empty);
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

			_logToFile?.DeInit();
		}

		private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
		{
			lock (LastReceivedSnapshotLock)
				if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
					_lastReceivedSnapshot = elympicsSnapshot;

			if (!_started)
				StartClient();
		}

		private void StartClient() => _started = true;

		protected override bool ShouldDoElympicsUpdate() => Initialized && _started;

		protected override void ElympicsFixedUpdate()
		{
			ElympicsSnapshot receivedSnapshot;
			lock (LastReceivedSnapshotLock)
				receivedSnapshot = _lastReceivedSnapshot;

			_clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick, _lastPredictedTick, _lastDelayedInputTick, receivedSnapshot.TickStartUtc, TickStartUtc);

			_predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

			if (_clientTickCalculator.Results.CanPredict)
				using (ElympicsMarkers.Elympics_ProcessingInputMarker.Auto())
					ProcessInput();

			if (Config.Prediction)
			{
				using (ElympicsMarkers.Elympics_ReconcileLoopMarker.Auto())
					ReconcileIfRequired(receivedSnapshot);

				using (ElympicsMarkers.Elympics_PredictionMarker.Auto())
					if (_clientTickCalculator.Results.CanPredict)
					{
						Tick = _clientTickCalculator.Results.PredictionTick;

						using (ElympicsMarkers.Elympics_ApplyUnpredictablePartOfSnapshotMarker.Auto())
							ApplyUnpredictablePartOfSnapshot(receivedSnapshot);

						elympicsBehavioursManager.CommitVars();

						using (ElympicsMarkers.Elympics_ApplyingInputMarker.Auto())
							ApplyPredictedInput();

						using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
							elympicsBehavioursManager.ElympicsUpdate();

						using (ElympicsMarkers.Elympics_ProcessSnapshotMarker.Auto())
							ProcessSnapshot(Tick);

						_lastPredictedTick = Tick;
					}
			}
			else
			{
				ApplyFullSnapshot(receivedSnapshot);
				elympicsBehavioursManager.CommitVars();
			}

			ElympicsUpdateDuration = 1 / _clientTickCalculator.Results.ElympicsUpdateTickRate;

			if (Config.DetailedNetworkLog)
			{
				LogNetworkConditionsInInterval();
				_logToFile?.LogNetworkDetailsToFile(_clientTickCalculator.Results);
			}
		}

		private void LogNetworkConditionsInInterval()
		{
			if (!_lastClientPrintNetworkConditions.HasValue)
				_lastClientPrintNetworkConditions = TickStartUtc;

			if (!((TickStartUtc - _lastClientPrintNetworkConditions.Value).TotalSeconds > networkConditionsLogInterval))
				return;

			Debug.Log($"[Elympics] {_clientTickCalculator.Results}");
			_lastClientPrintNetworkConditions = TickStartUtc;
		}

		private void ProcessSnapshot(long predictionTick)
		{
			ElympicsSnapshot snapshot;

			snapshot = elympicsBehavioursManager.GetLocalSnapshot();

			snapshot.Tick = predictionTick;
			_predictionBuffer.AddSnapshotToBuffer(snapshot);
		}

		private void ProcessInput()
		{
			ElympicsInput input;

			using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
				input = elympicsBehavioursManager.OnInputForClient();

			AddMetadataToInput(input);
			_lastDelayedInputTick = _clientTickCalculator.Results.DelayedInputTick;
			SendInput(input);
			_predictionBuffer.AddInputToBuffer(input);
		}

		private void AddMetadataToInput(ElympicsInput input)
		{
			input.Tick = _clientTickCalculator.Results.DelayedInputTick;
			input.Player = Player;
		}

		private void SendInput(ElympicsInput input) => _matchClient.SendInputUnreliable(input);

		private void ApplyPredictedInput()
		{
			_inputList.Clear();
			if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.Results.PredictionTick, out var predictedInput))
				_inputList.Add(predictedInput);
			elympicsBehavioursManager.SetCurrentInputs(_inputList);
		}

		private void ApplyUnpredictablePartOfSnapshot(ElympicsSnapshot snapshot) => elympicsBehavioursManager.ApplySnapshot(snapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

		private void ReconcileIfRequired(ElympicsSnapshot receivedSnapshot)
		{
			if (Config.ReconciliationFrequency == ElympicsGameConfig.ReconciliationFrequencyEnum.Never)
				return;

			bool forceSnapShot = receivedSnapshot.Tick > Tick;

			ElympicsSnapshot historySnapshot = null;

			if (!forceSnapShot && !_predictionBuffer.TryGetSnapshotFromBuffer(receivedSnapshot.Tick, out historySnapshot))
				return;

			if (!forceSnapShot && elympicsBehavioursManager.AreSnapshotsEqualOnPredictableBehaviours(historySnapshot, receivedSnapshot) && Config.ReconciliationFrequency != ElympicsGameConfig.ReconciliationFrequencyEnum.OnEverySnapshot)
				return;

			if (forceSnapShot)
				historySnapshot = receivedSnapshot;

			elympicsBehavioursManager.OnPreReconcile();

			// Debug.Log($"[{_player}] Applying snapshot {_lastReceivedSnapshot.Tick} with {JsonConvert.SerializeObject(_lastReceivedSnapshot, Formatting.Indented)}");
			Tick = receivedSnapshot.Tick;
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable, true);
			elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);
			elympicsBehavioursManager.CommitVars();

			var currentSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
			currentSnapshot.Tick = receivedSnapshot.Tick;
			_predictionBuffer.AddOrReplaceSnapshotInBuffer(currentSnapshot);

			var startResimulation = _clientTickCalculator.Results.LastReceivedTick + 1;
			var endResimulation = _clientTickCalculator.Results.PredictionTick - 1;
			using (ElympicsMarkers.Elympics_ResimulationkMarker.Auto())
				for (var resimulationTick = startResimulation; resimulationTick <= endResimulation; resimulationTick++)
				{
					Tick = resimulationTick;
					if (_predictionBuffer.TryGetSnapshotFromBuffer(resimulationTick, out historySnapshot))
						elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);
					elympicsBehavioursManager.CommitVars();

					_inputList.Clear();
					if (_predictionBuffer.TryGetInputFromBuffer(resimulationTick, out var resimulatedInput))
						_inputList.Add(resimulatedInput);
					elympicsBehavioursManager.SetCurrentInputs(_inputList);

					using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
						elympicsBehavioursManager.ElympicsUpdate();

					var newResimulatedSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
					newResimulatedSnapshot.Tick = resimulationTick;
					_predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);
				}

			_clientTickCalculator.Results.ReconciliationPerformed = true;
			elympicsBehavioursManager.OnPostReconcile();
		}

		private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot)
		{
			Tick = receivedSnapshot.Tick;
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot);
		}

		#region IElympics

		public override IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)    => MatchConnectClient.ConnectAndJoinAsPlayer(connectedCallback, ct);
		public override IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectator(connectedCallback, ct);
		public override void        Disconnect()                                                                    => MatchConnectClient.Disconnect();

		#endregion
	}
}
