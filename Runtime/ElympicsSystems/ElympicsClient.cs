using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MatchTcpClients.Synchronizer;
using Unity.Profiling;
using UnityEngine;

namespace Elympics
{
	public class ElympicsClient : ElympicsBase
	{
		private const int MaxInputsToSendOnPredictionJump = 5;
		private const int MaxTicksToTickOnPredictionJump = 3;

		[SerializeField] private bool connectOnStart = true;

		[SerializeField, Range(1, 60), Tooltip("In seconds")]
		private int networkConditionsLogInterval = 5;

		private ElympicsPlayer _player;
		public override ElympicsPlayer Player => _player;
		public override bool IsClient => true;

		private bool _started;
#if ELYMPICS_DEBUG
		private ClientTickCalculatorNetworkDetailsToFile logToFile;
#endif
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
		private IMatchClient _matchClient;

		// Prediction
		private IRoundTripTimeCalculator _roundTripTimeCalculator;
		private ClientTickCalculator _clientTickCalculator;
		private PredictionBuffer _predictionBuffer;

		private static readonly object LastReceivedSnapshotLock = new object();
		private ElympicsSnapshot _lastReceivedSnapshot;

		private DateTime? _lastClientPrintNetworkConditions;

		private List<ElympicsInput> _inputList;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, IMatchConnectClient matchConnectClient, IMatchClient matchClient, InitialMatchPlayerData initialMatchPlayerData)
		{
			base.InitializeInternal(elympicsGameConfig);
			_player = initialMatchPlayerData.Player;
			_matchConnectClient = matchConnectClient;
			_matchClient = matchClient;

			elympicsBehavioursManager.InitializeInternal(this);
			_roundTripTimeCalculator = new RoundTripTimeCalculator(_matchClient, _matchConnectClient);
#if ELYMPICS_DEBUG
			logToFile = new ClientTickCalculatorNetworkDetailsToFile();
#endif
			_clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator);
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

#if ELYMPICS_DEBUG
			logToFile.DeInit();
#endif
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

		protected override bool ShouldDoFixedUpdate() => Initialized && _started;

		protected override void DoFixedUpdate()
		{
			_tickStartUtc = DateTime.UtcNow;

			ElympicsSnapshot receivedSnapshot;
			lock (LastReceivedSnapshotLock)
				receivedSnapshot = _lastReceivedSnapshot;

			var networkDetails = _clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick, receivedSnapshot.TickStartUtc, _tickStartUtc);
			if (Config.DetailedNetworkLog)
			{
				LogNetworkConditionsInInterval(networkDetails);
#if ELYMPICS_DEBUG
				logToFile.LogNetworkDetailsToFile(networkDetails);
#endif
			}

			_predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

			var lastDelayedInputTick = _clientTickCalculator.LastDelayedInputTick;
			var delayedInputTick = _clientTickCalculator.DelayedInputTick;

			var startDelayedInputTick = Math.Max(lastDelayedInputTick + 1, delayedInputTick - MaxInputsToSendOnPredictionJump);

			using (ElympicsMarkers.Elympics_ProcessingInputMarker.Auto())
				for (var i = startDelayedInputTick; i <= delayedInputTick; i++)
					ProcessInput(i);

			if (Config.Prediction)
			{
				using (ElympicsMarkers.Elympics_ReconcileLoopMarker.Auto())
					ReconcileIfRequired(receivedSnapshot);

				var lastPredictionTick = _clientTickCalculator.LastPredictionTick;
				var predictionTick = _clientTickCalculator.PredictionTick;
				var tickDiff = predictionTick - (lastPredictionTick + 1);
				var startPredictionTick = tickDiff <= MaxTicksToTickOnPredictionJump ? lastPredictionTick + 1 : predictionTick;

				using (ElympicsMarkers.Elympics_PredictionMarker.Auto())
					for (var i = startPredictionTick; i <= predictionTick; i++)
					{
						Tick = i;
						_tickStartUtc = DateTime.UtcNow;

						using (ElympicsMarkers.Elympics_ApplyUnpredictablePartOfSnapshotMarker.Auto())
							ApplyUnpredictablePartOfSnapshot(receivedSnapshot);

						elympicsBehavioursManager.CommitVars();

						using (ElympicsMarkers.Elympics_ApplyingInputMarker.Auto())
							ApplyPredictedInput();

						using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
							elympicsBehavioursManager.ElympicsUpdate();

						using (ElympicsMarkers.Elympics_ProcessSnapshotMarker.Auto())
							ProcessSnapshot(i);
					}
			}
			else
			{
				ApplyFullSnapshot(receivedSnapshot);
				elympicsBehavioursManager.CommitVars();
			}
		}

		private void LogNetworkConditionsInInterval(ClientTickCalculatorNetworkDetails details)
		{
			if (!_lastClientPrintNetworkConditions.HasValue)
				_lastClientPrintNetworkConditions = _tickStartUtc;

			if (!((_tickStartUtc - _lastClientPrintNetworkConditions.Value).TotalSeconds > networkConditionsLogInterval) && !details.ForcedTickJump)
				return;

			Debug.Log($"[Elympics] {details.ToString()}");
			_lastClientPrintNetworkConditions = _tickStartUtc;
		}

		private void ProcessSnapshot(long predictionTick)
		{
			ElympicsSnapshot snapshot;

			snapshot = elympicsBehavioursManager.GetLocalSnapshot();

			snapshot.Tick = predictionTick;
			_predictionBuffer.AddSnapshotToBuffer(snapshot);
		}

		private void ProcessInput(long delayedInputTick)
		{
			ElympicsInput input;

			using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
				input = elympicsBehavioursManager.OnInputForClient();

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

			using (ElympicsMarkers.Elympics_ResimulationkMarker.Auto())
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

					using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
						elympicsBehavioursManager.ElympicsUpdate();

					var newResimulatedSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
					newResimulatedSnapshot.Tick = resimulationTick;
					_predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);

					// Debug.Log($"[{PlayerId}] Overriding snapshot {resimulationTick} with {JsonConvert.SerializeObject(newResimulatedSnapshot, Formatting.Indented)}");
				}

			elympicsBehavioursManager.OnPostReconcile();

			Debug.LogWarning($"[Elympics] <<< Reconciliation on {receivedSnapshot.Tick} finished <<<");
		}

		private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot)
		{
			Tick = receivedSnapshot.Tick;
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot);
		}

		#region IElympics

		public override IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsPlayer(connectedCallback, ct);
		public override IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectator(connectedCallback, ct);
		public override void Disconnect() => MatchConnectClient.Disconnect();

		#endregion
	}
}