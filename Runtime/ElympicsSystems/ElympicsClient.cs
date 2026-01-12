#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Elympics.AssemblyCommunicator;
using Elympics.AssemblyCommunicator.Events;
using Elympics.ElympicsSystems.Internal;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.ElympicsClient)]
    public partial class ElympicsClient : ElympicsBase
    {
        [SerializeField] private bool connectOnStart = true;

        [SerializeField, Range(0, 60), Tooltip("In seconds")]
        private int networkConditionsLogInterval = 5;

        private const uint PredictionBlockedThreshold = 10;
        private ElympicsPlayer _player;
        public override ElympicsPlayer Player => _player;
        public override bool IsClient => true;

        private long _tick;
        public override long Tick => _tick;

        /// <summary>Raised whenever <see cref="TimeSynchronizationData"/> is generated passing it as argument together with current tick.</summary>
        public static event Action<TimeSynchronizationData, long> TimeSynchronized;

        private bool _started;
        private ClientTickCalculatorNetworkDetailsToFile _logToFile;
        internal IMatchConnectClient MatchConnectClient => _matchConnectClient ?? throw new ElympicsException("Elympics not initialized! Did you change ScriptExecutionOrder?");
        private IMatchConnectClient _matchConnectClient;
        private IMatchClient _matchClient;

        // Prediction
        private IRoundTripTimeCalculator _roundTripTimeCalculator;
        private ClientTickCalculator _clientTickCalculator;
        private PredictionBuffer _predictionBuffer;

        private static readonly object LastReceivedSnapshotLock = new();
        private ElympicsSnapshot _lastReceivedSnapshot;
        private long _latestReconciliationBaseSnapshotTick;
        private readonly ElympicsSnapshot _mergedSnapshot = new();

        private DateTime? _lastClientPrintNetworkConditions;
        private uint _currentTicksWithoutPrediction;
        private long _previousTick;
        private long _lastDelayedInputTick;

        private List<ElympicsInput> _inputList;

        private ElympicsBehaviourFirstSnapshotTracker _snapshotTracker;

        protected override double MaxUpdateTimeWarningThreshold => 1 / Config.MaxTickRate;

        private ElympicsLoggerContext _logger;

        internal void InitializeInternal(
            ElympicsGameConfig elympicsGameConfig,
            IMatchConnectClient matchConnectClient,
            IMatchClient matchClient,
            InitialMatchPlayerDataGuid initialMatchPlayerData,
            ElympicsBehavioursManager elympicsBehavioursManager,
            ElympicsLoggerContext logger)
        {
            _logger = logger.WithContext(nameof(ElympicsClient));
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            _player = initialMatchPlayerData.Player;
            _matchConnectClient = matchConnectClient;
            _matchClient = matchClient;
            elympicsBehavioursManager.InitializeInternal(this);
            _roundTripTimeCalculator = new RoundTripTimeCalculator();
#if ELYMPICS_DEBUG
            _logToFile = new ClientTickCalculatorNetworkDetailsToFile();
#endif
            _clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator, elympicsGameConfig);
            _predictionBuffer = new PredictionBuffer(elympicsGameConfig);
            _snapshotTracker = new ElympicsBehaviourFirstSnapshotTracker(elympicsBehavioursManager);

            SetupCallbacks();
            OnStandaloneClientInit(initialMatchPlayerData);

            _inputList = new List<ElympicsInput>(1);

            SetInitialized();

            if (connectOnStart)
                StartCoroutine(ConnectAndJoinAsPlayer(success =>
                    {
                        var log = _logger.WithMethodName();
                        if (success)
                            log.Log("Successfully connected to the game server.");
                        else
                            log.Error("Could not connect to the game server.");
                    },
                    default));
        }

        private void SetupCallbacks()
        {
            _matchClient.SnapshotReceived += OnSnapshotReceived;
            _matchClient.Synchronized += OnMatchClientSynchronized;
            _matchClient.RpcMessageListReceived += QueueRpcMessagesToInvoke;
            _matchConnectClient.DisconnectedByServer += OnDisconnectedByServer;
            _matchConnectClient.DisconnectedByClient += OnDisconnectedByClient;
            _matchConnectClient.ConnectedWithSynchronizationData += OnConnectedWithSynchronizationData;
            _matchConnectClient.ConnectingFailed += OnConnectingFailed;
            _matchConnectClient.AuthenticatedUserMatchWithUserId += OnAuthenticated;
            _matchConnectClient.AuthenticatedUserMatchFailedWithError += OnAuthenticatedFailed;
            _matchConnectClient.AuthenticatedAsSpectator += () => OnAuthenticated(Guid.Empty);
            _matchConnectClient.AuthenticatedAsSpectatorWithError += OnAuthenticatedFailed;
            _matchConnectClient.MatchJoinedWithMatchInitData += OnMatchJoinedWithInitData;
            _matchConnectClient.MatchJoinedWithError += OnMatchJoinedFailed;
            _matchConnectClient.MatchEndedWithMatchId += OnMatchEnded;
        }

        private void OnConnectedWithSynchronizationData(TimeSynchronizationData data)
        {
            _roundTripTimeCalculator.OnSynchronized(data);
            RaiseRttReceived(data);
            OnConnected(data);
        }

        private void OnMatchClientSynchronized(TimeSynchronizationData data)
        {
            OnSynchronized(data);
            RaiseRttReceived(data);
            TimeSynchronized?.Invoke(data, Tick);
        }

        private void RaiseRttReceived(TimeSynchronizationData data) => CrossAssemblyEventBroadcaster.RaiseEvent(new RttReceived() { rtt = (float)data.RoundTripDelay.TotalMilliseconds, tick = Tick });

        private void OnDestroy()
        {
            if (_matchClient != null)
            {
                _matchClient.SnapshotReceived -= OnSnapshotReceived;
                _matchClient.Synchronized -= OnMatchClientSynchronized;
                _matchClient.RpcMessageListReceived -= QueueRpcMessagesToInvoke;
                _matchClient.Dispose();
            }

            _logToFile?.DeInit();
        }

        private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
        {
            lock (LastReceivedSnapshotLock)
                if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
                {
                    _lastReceivedSnapshot = elympicsSnapshot;
                    _matchClient.SetLastReceivedSnapshot(elympicsSnapshot.Tick);
                }

            if (!_started)
                StartClient();
        }

        private void StartClient() => _started = true;

        protected override bool ShouldDoElympicsUpdate() => Initialized && _started;

        internal override void ElympicsFixedUpdate()
        {
            var receivedSnapshot = _lastReceivedSnapshot;

            _clientTickCalculator.CalculateNextTick(receivedSnapshot.Tick, _previousTick, _lastDelayedInputTick, receivedSnapshot.TickStartUtc, TickStartUtc);

            _predictionBuffer.UpdateMinTick(receivedSnapshot.Tick);

            if (_clientTickCalculator.Results.CanPredict)
                using (ElympicsMarkers.Elympics_ProcessingInputMarker.Auto())
                    ProcessInput();

            SendBufferInput(Tick);

            _snapshotTracker.ProcessNewSnapshot(receivedSnapshot);

            CheckIfPredictionIsBlocked();

            using (ElympicsMarkers.Elympics_ReconcileLoopMarker.Auto())
                ReconcileIfRequired(_latestReconciliationBaseSnapshotTick == receivedSnapshot.Tick
                    ? null
                    : receivedSnapshot);
            _latestReconciliationBaseSnapshotTick = receivedSnapshot.Tick;

            using (ElympicsMarkers.Elympics_PredictionMarker.Auto())
                if (_clientTickCalculator.Results.CanPredict)
                {
                    _tick = _clientTickCalculator.Results.CurrentTick;

                    using (ElympicsMarkers.Elympics_ApplyUnpredictablePartOfSnapshotMarker.Auto())
                        ApplyUnpredictablePartOfSnapshot(receivedSnapshot);

                    _snapshotTracker.InitializeNewBehaviours();

                    InvokeQueuedRpcMessages();
                    ElympicsBehavioursManager.CommitVars();

                    using (ElympicsMarkers.Elympics_ApplyingInputMarker.Auto())
                        ApplyPredictedInput();

                    using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
                        ElympicsBehavioursManager.ElympicsUpdate();

                    using (ElympicsMarkers.Elympics_ProcessSnapshotMarker.Auto())
                        ProcessSnapshot(Tick);

                    _previousTick = Tick;
                }

            ElympicsUpdateDuration = 1 / _clientTickCalculator.Results.ElympicsUpdateTickRate;

            SendQueuedRpcMessages();

            if (Config.DetailedNetworkLog)
            {
                LogNetworkConditionsInInterval();
                _logToFile?.LogNetworkDetailsToFile(_clientTickCalculator.Results);
            }
        }

        protected override void ElympicsRenderUpdate(in RenderData renderData) => ElympicsBehavioursManager.Render(renderData);

        private void CheckIfPredictionIsBlocked()
        {
            var rttMs = _clientTickCalculator.Results.RttTicks * Config.TickDuration * 1000;
            var lcoMs = _clientTickCalculator.Results.LcoTicks * Config.TickDuration * 1000;

            if (_clientTickCalculator.Results.CanPredict)
            {
                if (_currentTicksWithoutPrediction >= PredictionBlockedThreshold)
                {
                    ElympicsLogger.LogWarning($"Prediction unblocked after {_currentTicksWithoutPrediction} ticks. " + $"Check your Internet connection. Current RTT: {rttMs} ms, LCO: {lcoMs} ms");
                    PredictionStateChanged(false, _clientTickCalculator.Results);
                }
                _currentTicksWithoutPrediction = 0;
                return;
            }
            if (++_currentTicksWithoutPrediction != PredictionBlockedThreshold)
                return;

            ElympicsLogger.LogWarning("Prediction is blocked, probably due to a lag spike. " + $"Check your Internet connection. Current RTT: {rttMs} ms, LCO: {lcoMs} ms");
            PredictionStateChanged(true, _clientTickCalculator.Results);
        }

        private void LogNetworkConditionsInInterval()
        {
            if (!_lastClientPrintNetworkConditions.HasValue)
                _lastClientPrintNetworkConditions = TickStartUtc;

            if (!((TickStartUtc - _lastClientPrintNetworkConditions.Value).TotalSeconds > networkConditionsLogInterval))
                return;

            ElympicsLogger.Log(_clientTickCalculator.Results.ToString());
            _lastClientPrintNetworkConditions = TickStartUtc;
        }

        private void ProcessSnapshot(long predictionTick)
        {
            var snapshot = ElympicsBehavioursManager.GetLocalSnapshot();
            snapshot.Tick = predictionTick;
            _ = _predictionBuffer.AddSnapshotToBuffer(snapshot);
        }

        private void ProcessInput()
        {
            ElympicsInput input;

            using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
                input = ElympicsBehavioursManager.OnInputForClient();

            AddMetadataToInput(input);
            _lastDelayedInputTick = _clientTickCalculator.Results.DelayedInputTick;
            AddInputToSendBuffer(input);
            _ = _predictionBuffer.AddInputToBuffer(input);
        }

        private void AddMetadataToInput(ElympicsInput input)
        {
            input.Tick = _clientTickCalculator.Results.DelayedInputTick;
            input.Player = Player;
        }

        private void AddInputToSendBuffer(ElympicsInput input) => _matchClient.AddInputToSendBuffer(input);

        private void SendBufferInput(long tick) => _matchClient.SendBufferInput(tick);

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            _matchClient.SendRpcMessageList(rpcMessageList);

        private void ApplyPredictedInput()
        {
            _inputList.Clear();
            if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.Results.CurrentTick, out var predictedInput))
                _inputList.Add(predictedInput);
            ElympicsBehavioursManager.SetCurrentInputs(_inputList);
        }

        private void ApplyUnpredictablePartOfSnapshot(ElympicsSnapshot snapshot) => ElympicsBehavioursManager.ApplySnapshot(snapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

        private void ReconcileIfRequired(ElympicsSnapshot? receivedSnapshot)
        {
            if (receivedSnapshot == null)
                return;

            if (Config.ReconciliationFrequency == ElympicsGameConfig.ReconciliationFrequencyEnum.Never)
                return;

            var forceSnapShot = receivedSnapshot.Tick > Tick;

            ElympicsSnapshot? historySnapshot = null;
            ElympicsSnapshot newSnapshot;

            if (!forceSnapShot && !_predictionBuffer.TryGetSnapshotFromBuffer(receivedSnapshot.Tick, out historySnapshot))
            {
                _logger.WithMethodName()
                    .Warning(
                        $"Snapshot for {receivedSnapshot.Tick} was already dropped from the prediction buffer. Skipping reconciliation check.\nPrediction buffer size: {Config.PredictionBufferSize}\nTotal prediction limit: {Config.TotalPredictionLimitInTicks}.");
                return;
            }

            if (!forceSnapShot
                && ElympicsBehavioursManager.AreSnapshotsEqualOnPredictableBehaviours(historySnapshot, receivedSnapshot)
                && Config.ReconciliationFrequency != ElympicsGameConfig.ReconciliationFrequencyEnum.OnEverySnapshot)
                return;

            if (forceSnapShot)
            {
                //TO DO: Forcing should be triggered by server which should send a full snapshot when forcing jump forward
                //Not all snapshots sent by server contain data about all objects, so current implementation will only correctly set
                //data for objects that happen to be in this snapshot
                historySnapshot = receivedSnapshot;
                newSnapshot = receivedSnapshot;
                _logger.WithMethodName().Warning($"Forcing reconciliation to tick {receivedSnapshot.Tick} as it is higher than current tick {Tick}.");
            }
            else
            {
                //Not all snapshots sent by server contain data about all objects, but if we want to go back to a previous tick to resimulate
                //we have to revert states of all objects, so we take missing data from local snapshot
                _mergedSnapshot.DeepCopyFrom(receivedSnapshot);
                _mergedSnapshot.FillMissingFrom(historySnapshot);
                newSnapshot = _mergedSnapshot;
            }

            ElympicsBehavioursManager.OnPreReconcile();

            _tick = receivedSnapshot.Tick;

            ElympicsBehavioursManager.ApplySnapshot(newSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable, true);
            ElympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);

            var startResimulation = _clientTickCalculator.Results.LastReceivedTick + 1;
            var endResimulation = _clientTickCalculator.Results.CurrentTick - 1;
            _tick = startResimulation;
            _snapshotTracker.ProcessNewSnapshot(receivedSnapshot);
            _snapshotTracker.InitializeNewBehaviours();
            ElympicsBehavioursManager.CommitVars();

            var currentSnapshot = ElympicsBehavioursManager.GetLocalSnapshot();
            currentSnapshot.Tick = receivedSnapshot.Tick;
            _ = _predictionBuffer.AddOrReplaceSnapshotInBuffer(currentSnapshot);

            using (ElympicsMarkers.Elympics_ResimulationkMarker.Auto())
                for (var resimulationTick = startResimulation; resimulationTick <= endResimulation; resimulationTick++)
                {
                    _tick = resimulationTick;
                    if (_predictionBuffer.TryGetSnapshotFromBuffer(resimulationTick, out historySnapshot))
                        ElympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);
                    ElympicsBehavioursManager.CommitVars();

                    _inputList.Clear();
                    if (_predictionBuffer.TryGetInputFromBuffer(resimulationTick, out var resimulatedInput))
                        _inputList.Add(resimulatedInput);
                    ElympicsBehavioursManager.SetCurrentInputs(_inputList);

                    using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
                        ElympicsBehavioursManager.ElympicsUpdate();

                    var newResimulatedSnapshot = ElympicsBehavioursManager.GetLocalSnapshot();
                    newResimulatedSnapshot.Tick = resimulationTick;
                    _ = _predictionBuffer.AddOrReplaceSnapshotInBuffer(newResimulatedSnapshot);
                }

            _clientTickCalculator.Results.ReconciliationPerformed = true;
            ElympicsBehavioursManager.OnPostReconcile();
        }

        private void PredictionStateChanged(bool isBlocked, ClientTickCalculatorNetworkDetails results) => ElympicsBehavioursManager.OnPredictionStatusChanged(isBlocked, results);

        private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot)
        {
            _tick = receivedSnapshot.Tick;
            ElympicsBehavioursManager.ApplySnapshot(receivedSnapshot);
        }

        #region IElympics

        public override IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsPlayer(connectedCallback, ct);
        public override IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectator(connectedCallback, ct);
        public override void Disconnect() => MatchConnectClient.Disconnect();

        #endregion
    }
}
