#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.AssemblyCommunicator;
using Elympics.AssemblyCommunicator.Events;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
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
        public static event Action<TimeSynchronizationData, long>? TimeSynchronized;

        private volatile bool _started;
        private volatile bool _wasEverStarted;
        private volatile bool _reconnectResetPending;
        private Action? _onAuthenticatedAsSpectator;
        private ClientTickCalculatorNetworkDetailsToFile _logToFile;
        internal IMatchConnectClient MatchConnectClient => _matchConnectClient ?? throw new ElympicsException("Elympics not initialized! Did you change ScriptExecutionOrder?");
        private IMatchConnectClient _matchConnectClient;
        private IMatchClient _matchClient;

        // Prediction
        private IRoundTripTimeCalculator _roundTripTimeCalculator;
        private ClientTickCalculator _clientTickCalculator;
        private PredictionBuffer _predictionBuffer;

        private static readonly object LastReceivedSnapshotLock = new();
        private ElympicsSnapshot? _lastReceivedSnapshot;
        private long _latestReconciliationBaseSnapshotTick;
        private readonly ElympicsSnapshot _serverWorldState = ElympicsSnapshot.CreateEmpty();

        private DateTime? _lastClientPrintNetworkConditions;
        private uint _currentTicksWithoutPrediction;
        private long _previousTick;
        private long _lastDelayedInputTick;

        private List<ElympicsInput> _inputList;

        private ElympicsBehaviourFirstSnapshotTracker _snapshotTracker;

        private readonly RxSnapshotTicksTracker _rxSnapshotTicksTracker = new(TimeSpan.FromSeconds(5));

        protected override double MaxUpdateTimeWarningThreshold => 1 / Config.MaxTickRate;

        private readonly LoggerConfig _logger = ElympicsLogger.WithElympicsGameService()
            .WithClass(typeof(ElympicsClient))
            .WithMonitoringEnabled();

        internal void InitializeInternal(
            ElympicsGameConfig elympicsGameConfig,
            IMatchConnectClient matchConnectClient,
            IMatchClient matchClient,
            InitialMatchPlayerDataGuid initialMatchPlayerData,
            ElympicsBehavioursManager elympicsBehavioursManager,
            int maxPlayerCount)
        {
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            _rxSnapshotTicksTracker.Initialize(elympicsGameConfig.TicksPerSecond);
            _player = initialMatchPlayerData.Player;
            _matchConnectClient = matchConnectClient;
            _matchClient = matchClient;
            elympicsBehavioursManager.InitializeInternal(this, maxPlayerCount);
            _roundTripTimeCalculator = new RoundTripTimeCalculator();
            _logToFile = new ClientTickCalculatorNetworkDetailsToFile();
            _clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator, elympicsGameConfig);
            _predictionBuffer = new PredictionBuffer(elympicsGameConfig);
            _snapshotTracker = new ElympicsBehaviourFirstSnapshotTracker(elympicsBehavioursManager);

            SetupCallbacks();
            OnStandaloneClientInit(initialMatchPlayerData);

            _inputList = new List<ElympicsInput>(1);

            SetInitialized();

            if (connectOnStart)
                RunConnectAndJoinAsPlayer();
        }

        private async void RunConnectAndJoinAsPlayer()
        {
            var logger = _logger.WithMethodName();
            try
            {
                await UniTask.Yield();
                await ConnectAndJoinAsPlayerAsync(CancellationToken.None);
                logger.LogInfo("Successfully connected to the game server.");
            }
            catch (OperationCanceledException)
            {
                logger.LogInfo("Connect and join was cancelled.");
            }
            catch (Exception e)
            {
                logger.LogException(new ElympicsException("Could not connect to the game server.", e));
            }
        }

        private void SetupCallbacks()
        {
            _matchClient.SnapshotReceived += OnSnapshotReceived;
            _matchClient.Synchronized += OnMatchClientSynchronized;
            _matchClient.RpcMessageListReceived += QueueRpcMessagesFromServerToInvoke;
            _matchConnectClient.DisconnectedByServer += OnDisconnectedByServerHandler;
            _matchConnectClient.DisconnectedByClient += OnDisconnectedByClientHandler;
            _matchConnectClient.ConnectedWithSynchronizationData += OnConnectedWithSynchronizationData;
            _matchConnectClient.ConnectingFailed += OnConnectingFailed;
            _matchConnectClient.AuthenticatedUserMatchWithUserId += OnAuthenticated;
            _matchConnectClient.AuthenticatedUserMatchFailedWithError += OnAuthenticatedFailed;
            _onAuthenticatedAsSpectator = () => OnAuthenticated(Guid.Empty);
            _matchConnectClient.AuthenticatedAsSpectator += _onAuthenticatedAsSpectator;
            _matchConnectClient.AuthenticatedAsSpectatorWithError += OnAuthenticatedFailed;
            _matchConnectClient.MatchJoinedWithMatchInitData += OnMatchJoinedWithInitData;
            _matchConnectClient.MatchJoinedWithError += OnMatchJoinedFailed;
            _matchConnectClient.MatchEndedWithMatchId += OnMatchEnded;
        }

        private void OnConnectedWithSynchronizationData(TimeSynchronizationData data)
        {
            _roundTripTimeCalculator.OnSynchronized(data);
            RaiseRttReceived(data);
            RaiseReceivedStatsUpdated();
            OnConnected(data);
        }

        private void OnMatchClientSynchronized(TimeSynchronizationData data)
        {
            _roundTripTimeCalculator.OnSynchronized(data);
            OnSynchronized(data);
            RaiseRttReceived(data);
            RaiseReceivedStatsUpdated();
            TimeSynchronized?.Invoke(data, Tick);
        }

        private void RaiseRttReceived(TimeSynchronizationData data) => CrossAssemblyEventBroadcaster.RaiseEvent(new RttReceived
        {
            rtt = (float)data.RoundTripDelay.TotalMilliseconds,
            tick = Tick,
        });

        private void RaiseReceivedStatsUpdated() => CrossAssemblyEventBroadcaster.RaiseEvent(_rxSnapshotTicksTracker.CurrentState);

        private void OnDestroy()
        {
            if (_matchConnectClient != null)
            {
                _matchConnectClient.DisconnectedByServer -= OnDisconnectedByServerHandler;
                _matchConnectClient.DisconnectedByClient -= OnDisconnectedByClientHandler;
                _matchConnectClient.ConnectedWithSynchronizationData -= OnConnectedWithSynchronizationData;
                _matchConnectClient.ConnectingFailed -= OnConnectingFailed;
                _matchConnectClient.AuthenticatedUserMatchWithUserId -= OnAuthenticated;
                _matchConnectClient.AuthenticatedUserMatchFailedWithError -= OnAuthenticatedFailed;
                _matchConnectClient.AuthenticatedAsSpectator -= _onAuthenticatedAsSpectator;
                _matchConnectClient.AuthenticatedAsSpectatorWithError -= OnAuthenticatedFailed;
                _matchConnectClient.MatchJoinedWithMatchInitData -= OnMatchJoinedWithInitData;
                _matchConnectClient.MatchJoinedWithError -= OnMatchJoinedFailed;
                _matchConnectClient.MatchEndedWithMatchId -= OnMatchEnded;
            }

            if (_matchClient != null)
            {
                _matchClient.SnapshotReceived -= OnSnapshotReceived;
                _matchClient.Synchronized -= OnMatchClientSynchronized;
                _matchClient.RpcMessageListReceived -= QueueRpcMessagesFromServerToInvoke;
                _matchClient.Dispose();
            }

            _logToFile.DeInit();
        }

        private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
        {
            lock (LastReceivedSnapshotLock)
            {
                _rxSnapshotTicksTracker.Update(elympicsSnapshot.Tick);
                if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
                {
                    _lastReceivedSnapshot = elympicsSnapshot;
                    _matchClient.SetLastReceivedSnapshot(elympicsSnapshot.Tick);
                }
                if (_started || _reconnectResetPending)
                    return;
                if (_wasEverStarted)
                {
                    _reconnectResetPending = true;
                    Enqueue(() =>
                    {
                        if (this == null)
                            return; // MonoBehaviour destroyed during scene unload
                        _reconnectResetPending = false;
                        if (_started)
                            return;
                        ResetForReconnect();
                        StartClient();
                    });
                }
                else
                    StartClient();
            }
        }

        private void StartClient()
        {
            _started = true;
            _wasEverStarted = true;
        }

        private void StopClient() => _started = false;

        private void OnDisconnectedByServerHandler()
        {
            StopClient();
            OnDisconnectedByServer();
        }

        private void OnDisconnectedByClientHandler()
        {
            StopClient();
            OnDisconnectedByClient();
        }

        private void ResetForReconnect()
        {
            var log = _logger.WithMethodName();
            log.LogInfo("Resetting for reconnect...");

            ElympicsBehavioursManager.DestroyAllDynamicInstances();
            ElympicsBehavioursManager.ResetWorldAndReRegisterSceneObjects();

            _tick = -1;
            _lastReceivedSnapshot = null;
            _rxSnapshotTicksTracker.Clear();
            _latestReconciliationBaseSnapshotTick = -1;
            _serverWorldState.ResetToEmpty();
            _currentTicksWithoutPrediction = 0;
            _previousTick = 0;
            _lastDelayedInputTick = 0;
            _lastClientPrintNetworkConditions = null;
            _inputList.Clear();

            _roundTripTimeCalculator = new RoundTripTimeCalculator();
            _clientTickCalculator = new ClientTickCalculator(_roundTripTimeCalculator, Config);
            _predictionBuffer = new PredictionBuffer(Config);
            _snapshotTracker = new ElympicsBehaviourFirstSnapshotTracker(ElympicsBehavioursManager);

            ResetRpcQueues();
            ResetTimer();

            log.LogInfo("Reconnect reset complete.");
        }

        protected override bool ShouldDoElympicsUpdate() => Initialized && _started;

        internal override void ElympicsFixedUpdate()
        {
            var receivedSnapshot = _lastReceivedSnapshot;
            _serverWorldState.MergeWithSnapshot(receivedSnapshot);

            _predictionBuffer.UpdateMinTick(_serverWorldState.Tick);

            ReconciliationResult reconciliationResult;
            using (ElympicsMarkers.Elympics_ReconcileLoopMarker.Auto())
                reconciliationResult = ReconcileIfRequired(_latestReconciliationBaseSnapshotTick == _serverWorldState.Tick
                    ? null
                    : _serverWorldState);
            _latestReconciliationBaseSnapshotTick = _serverWorldState.Tick;

            _clientTickCalculator.CalculateNextTick(_serverWorldState.Tick, _previousTick, _lastDelayedInputTick, _serverWorldState.TickStartUtc, TickStartUtc, reconciliationResult);


            if (_clientTickCalculator.Results.CanPredict)
                using (ElympicsMarkers.Elympics_ProcessingInputMarker.Auto())
                    ProcessInput();

            SendBufferInput(Tick);

            _snapshotTracker.ProcessNewSnapshot(_serverWorldState);

            CheckIfPredictionIsBlocked();


            using (ElympicsMarkers.Elympics_PredictionMarker.Auto())
                if (_clientTickCalculator.Results.CanPredict)
                {
                    _tick = _clientTickCalculator.Results.CurrentTick;

                    using (ElympicsMarkers.Elympics_ApplyUnpredictablePartOfSnapshotMarker.Auto())
                        ApplyUnpredictablePartOfSnapshot(_serverWorldState);

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
                _logToFile.LogNetworkDetailsToFile(_clientTickCalculator.Results);
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
                    ElympicsLogger.LogWarning($"Prediction unblocked after {_currentTicksWithoutPrediction} ticks. "
                        + $"Check your Internet connection. Current RTT: {rttMs} ms, LCO: {lcoMs} ms");
                    PredictionStateChanged(false, _clientTickCalculator.Results);
                }

                _currentTicksWithoutPrediction = 0;
                return;
            }

            if (++_currentTicksWithoutPrediction != PredictionBlockedThreshold)
                return;

            ElympicsLogger.LogWarning("Prediction is blocked, probably due to a lag spike. "
                + $"Check your Internet connection. Current RTT: {rttMs} ms, LCO: {lcoMs} ms");
            PredictionStateChanged(true, _clientTickCalculator.Results);
        }

        private void LogNetworkConditionsInInterval()
        {
            if (!ScriptingSymbols.IsElympicsDebug && !Config.DetailedNetworkLog)
                return;
            _lastClientPrintNetworkConditions ??= TickStartUtc;
            if (!((TickStartUtc - _lastClientPrintNetworkConditions.Value).TotalSeconds > networkConditionsLogInterval))
                return;
            var message = _clientTickCalculator.Results.ToString();
            _lastClientPrintNetworkConditions = TickStartUtc;
            ElympicsLogger.WithMonitoringEnabled()
                .WithConsoleDisabled()
                .LogDebug(message);
            if (Config.DetailedNetworkLog)
                ElympicsLogger.LogInfo(message);
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

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList, bool reliable) =>
            _matchClient.SendRpcMessageList(rpcMessageList, reliable);

        private void ApplyPredictedInput()
        {
            _inputList.Clear();
            if (_predictionBuffer.TryGetInputFromBuffer(_clientTickCalculator.Results.CurrentTick, out var predictedInput))
                _inputList.Add(predictedInput);
            ElympicsBehavioursManager.SetCurrentInputs(_inputList);
        }

        private void ApplyUnpredictablePartOfSnapshot(ElympicsSnapshot snapshot) => ElympicsBehavioursManager.ApplySnapshot(snapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable);

        private ReconciliationResult ReconcileIfRequired(ElympicsSnapshot? receivedSnapshot)
        {
            if (receivedSnapshot == null)
                return ReconciliationResult.None;

            if (Config.ReconciliationFrequency == ElympicsGameConfig.ReconciliationFrequencyEnum.Never)
                return ReconciliationResult.None;

            var clientWasBehind = receivedSnapshot.Tick > Tick;

            ElympicsSnapshot? historySnapshot = null;
            ElympicsSnapshot newSnapshot;

            switch (clientWasBehind)
            {
                case false when !_predictionBuffer.TryGetSnapshotFromBuffer(receivedSnapshot.Tick, out historySnapshot):
                    _logger.WithMethodName()
                        .LogWarning(
                            $"Snapshot for {receivedSnapshot.Tick} was already dropped from the prediction buffer. Skipping reconciliation check.\nPrediction buffer size: {Config.PredictionBufferSize}\nTotal prediction limit: {Config.TotalPredictionLimitInTicks}.");
                    return ReconciliationResult.None;
                case false
                    when ElympicsBehavioursManager.AreSnapshotsEqualOnPredictableBehaviours(historySnapshot, receivedSnapshot)
                         && Config.ReconciliationFrequency != ElympicsGameConfig.ReconciliationFrequencyEnum.OnEverySnapshot:
                    return ReconciliationResult.None;
                case true:
                    //TO DO: Forcing should be triggered by server which should send a full snapshot when forcing jump forward
                    //Not all snapshots sent by server contain data about all objects, so current implementation will only correctly set
                    //data for objects that happen to be in this snapshot
                    historySnapshot = receivedSnapshot;
                    newSnapshot = receivedSnapshot;
                    _previousTick = receivedSnapshot.Tick;
                    _logger.WithMethodName().LogWarning($"Forcing reconciliation to tick {receivedSnapshot.Tick} as it is higher than current tick {Tick}.");
                    break;
                default:
                    newSnapshot = receivedSnapshot;
                    break;
            }

            ElympicsBehavioursManager.OnPreReconcile();

            _tick = receivedSnapshot.Tick;

            ElympicsBehavioursManager.ApplySnapshot(newSnapshot, ElympicsBehavioursManager.StatePredictability.Predictable, true);
            ElympicsBehavioursManager.ApplySnapshot(historySnapshot, ElympicsBehavioursManager.StatePredictability.Unpredictable, true);

            var startResimulation = receivedSnapshot.Tick + 1;
            var endResimulation = _previousTick;
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

            ElympicsBehavioursManager.OnPostReconcile();
            return clientWasBehind
                ? ReconciliationResult.Reanchored(receivedSnapshot.Tick)
                : ReconciliationResult.Replayed();
        }

        private void PredictionStateChanged(bool isBlocked, ClientTickCalculatorNetworkDetails results) => ElympicsBehavioursManager.OnPredictionStatusChanged(isBlocked, results);

        private void ApplyFullSnapshot(ElympicsSnapshot receivedSnapshot)
        {
            _tick = receivedSnapshot.Tick;
            ElympicsBehavioursManager.ApplySnapshot(receivedSnapshot);
        }

        #region IElympics

        public override UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsPlayerAsync(ct);
        public override UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct) => MatchConnectClient.ConnectAndJoinAsSpectatorAsync(ct);
        public override void Disconnect() => MatchConnectClient.Disconnect();

        #endregion
    }
}
