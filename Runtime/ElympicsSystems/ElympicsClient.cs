using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public class ElympicsClient : ElympicsBase
	{
		[SerializeField] private bool connectOnStart = true;

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

			Initialized = true;
			if (connectOnStart)
				StartCoroutine(ConnectAndJoinAsPlayer(_ => {}, default));
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
			var receivedSnapshot = _lastReceivedSnapshot;
			_clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick);
			_predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

			ProcessInput();

			if (Config.Prediction)
			{
				ReconcileIfRequired(receivedSnapshot);
				ApplyUnpredictablePartOfSnapshot(receivedSnapshot);
				ApplyPredictedInput();
			}
			else
			{
				ApplyFullSnapshot(receivedSnapshot);
			}
		}

		protected override void LateFixedUpdate() => ProcessSnapshot();

		private void ProcessSnapshot()
		{
			if (Config.Prediction)
			{
				var snapshot = elympicsBehavioursManager.GetLocalSnapshot();
				snapshot.Tick = _clientTickCalculator.PredictionTick;
				_predictionBuffer.AddSnapshotToBuffer(snapshot);
			}
		}

		private void ProcessInput()
		{
			var input = elympicsBehavioursManager.GetInputForClient();
			AddMetadataToInput(input);
			SendInput(input);
			_predictionBuffer.AddInputToBuffer(input);
		}

		private void AddMetadataToInput(ElympicsInput input)
		{
			input.Tick = (int) _clientTickCalculator.DelayedInputTick;
			input.Player = Player;
		}

		private void SendInput(ElympicsInput input) => _matchClient.SendInputUnreliable(input);

		private void ApplyPredictedInput()
		{
			if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.PredictionTick, out var predictedInput))
				elympicsBehavioursManager.ApplyInput(predictedInput);
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

			Debug.LogWarning($"[Player {Player}] Reconciliation on {receivedSnapshot.Tick}");

			elympicsBehavioursManager.OnPreReconcile();

			// Debug.Log($"[{_player}] Applying snapshot {_lastReceivedSnapshot.Tick} with {JsonConvert.SerializeObject(_lastReceivedSnapshot, Formatting.Indented)}");
			elympicsBehavioursManager.ApplySnapshot(receivedSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable, true);
			elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);

			var currentSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
			currentSnapshot.Tick = receivedSnapshot.Tick;
			_predictionBuffer.AddOrReplaceSnapshotInBuffer(currentSnapshot);

			for (var resimulationTick = receivedSnapshot.Tick + 1; resimulationTick < _clientTickCalculator.PredictionTick; resimulationTick++)
			{
				if (_predictionBuffer.TryGetSnapshotFromBuffer(resimulationTick, out historySnapshot))
					elympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);

				if (_predictionBuffer.TryGetInputFromBuffer(resimulationTick, out var resimulatedInput))
					elympicsBehavioursManager.ApplyInput(resimulatedInput);
				elympicsBehavioursManager.ElympicsUpdate();

				var newResimulatedSnapshot = elympicsBehavioursManager.GetLocalSnapshot();
				newResimulatedSnapshot.Tick = resimulationTick;
				_predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);
				// Debug.Log($"[{PlayerId}] Overriding snapshot {resimulationTick} with {JsonConvert.SerializeObject(newResimulatedSnapshot, Formatting.Indented)}");
			}

			elympicsBehavioursManager.OnPostReconcile();

			Debug.LogWarning($"[Player {Player}] Reconciliation on {receivedSnapshot.Tick} finished");
		}

		private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot) => elympicsBehavioursManager.ApplySnapshot(receivedSnapshot);

		#region IElympics

		public override IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)    => MatchConnectClient.ConnectAndJoinAsPlayer(connectedCallback, ct);
		public override IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectator(connectedCallback, ct);
		public override void        Disconnect()                                                                    => MatchConnectClient.Disconnect();

		#endregion
	}
}
