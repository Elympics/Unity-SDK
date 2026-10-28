#nullable enable
using System;
using Elympics.Core;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Serialization;
using UnityEngine;

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.ElympicsServer)]
    public class ElympicsServer : ElympicsBase
    {
        private static readonly ElympicsPlayer ServerPlayer = ElympicsPlayer.World;

        private ElympicsPlayer _currentPlayer = ElympicsPlayer.Invalid;
        private bool _currentIsBot;

        public override ElympicsPlayer Player => _currentPlayer;
        public override bool IsServer => true;
        public override bool IsBot => _currentIsBot;
        public override bool IsClient => HandlingClientsInServer || _isReplay;
        public override long Tick => _serverElympicsUpdate.Tick;

        public override bool IsReplay => _isReplay;

        private bool HandlingClientsInServer { get; set; }

        private bool _isReplay;

        private bool _endGameRequested;
        private bool _gameEnded;
        private ResultMatchPlayerDatas? _matchResult;
        private GameEngineAdapter? _gameEngineAdapter;
        private InitialMatchPlayerDatasGuid? _playerData;

        private SnapshotAnalysisCollector? _snapshotCollector;
        private IServerPlayerHandler _serverPlayerHandler = null!;
        private IServerElympicsUpdateLoop _serverElympicsUpdate = null!;
        private ElympicsSnapshotWithMetadata _currentSnapshot = null!;
        private ElympicsSnapshotWithMetadata? _previousSnapshot;
        private ElympicsScore? _score;
        private LoggerConfig _logger = ElympicsLogger.WithClass(typeof(ElympicsServer));

        internal void InitializeInternal(
            ElympicsGameConfig elympicsGameConfig,
            GameEngineAdapter gameEngineAdapter,
            ElympicsBehavioursManager elympicsBehavioursManager,
            IServerPlayerHandler playerHandler,
            SnapshotAnalysisCollector snapshotAnalysisCollector,
            IServerElympicsUpdateLoop serverElympicsUpdate,
            bool handlingClientsOverride = false)
        {
            _score = new ElympicsScore(elympicsGameConfig.MaxPlayers);
            _serverPlayerHandler = playerHandler;
            _snapshotCollector = snapshotAnalysisCollector;
            _serverElympicsUpdate = serverElympicsUpdate;
            _isReplay = _serverElympicsUpdate is SnapshotReplayElympicsUpdateLoop;
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            SwitchBehaviourToServer();
            HandlingClientsInServer = handlingClientsOverride;
            _gameEngineAdapter = gameEngineAdapter;
            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            if (_score != null)
                _score.PlayerScoreUpdated += SubmitIntermediateScore;
            if (_gameEngineAdapter is null)
                return;
            _gameEngineAdapter.PlayerConnected += OnPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected += OnPlayerDisconnected;
            _gameEngineAdapter.ReceivedInitialMatchPlayerDatas += args => Enqueue(() =>
            {
                _playerData = args.Data;
                _snapshotCollector?.Initialize(
                    Config,
                    _playerData,
                    new LatestMessagePackSerializer()
                );
                _logger.WithMethodName(nameof(_gameEngineAdapter.ReceivedInitialMatchPlayerDatas))
                    .LogDebug($"Received server seed: {args.Data.Seed?.ToString() ?? "none"}");
                ElympicsBehavioursManager.OnServerInit(args.Data);
                InitializeBotsAndClientInServer(args.Data);
                SetInitialized();
                args.OnInitialized();
            });
            _gameEngineAdapter.RpcMessageListReceived += QueueRpcMessagesToInvoke;
        }

        private void InitializeBotsAndClientInServer(InitialMatchPlayerDatasGuid data) => _serverPlayerHandler.InitializePlayersOnServer(data);

        protected override bool ShouldDoElympicsUpdate() => Initialized;

        internal override void ElympicsFixedUpdate()
        {
            using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
                _serverPlayerHandler.RetrieveInput(Tick);

            var snapshot = _serverElympicsUpdate.GenerateSnapshot();
            _currentSnapshot = ElympicsBehavioursManager.AddMetadataToSnapshot(snapshot);
        }

        protected override void ElympicsRenderUpdate(in RenderData renderData) => _serverElympicsUpdate.HandleRenderFrame(renderData);

        protected override void ElympicsLateFixedUpdate()
        {
            // TickEndUtc is set only once ElympicsFixedUpdate has returned: stamp and capture the snapshot here,
            // or it carries the end time of the previous tick.
            _currentSnapshot.TickEndUtc = TickEndUtc;
            using (ElympicsMarkers.Elympics_SnapshotCollector.Auto())
                _snapshotCollector?.CaptureSnapshot(_previousSnapshot, _currentSnapshot);
            _previousSnapshot = _currentSnapshot;

            SendQueuedRpcMessages();
            _score?.Submit();
            _serverElympicsUpdate.FinalizeTick(_currentSnapshot);
            if (_endGameRequested)
            {
                _endGameRequested = false;
                _gameEnded = true;
                SetDeInitialized();
                _snapshotCollector?.Dispose();
                _gameEngineAdapter?.EndGame(_matchResult);
            }

            if (_gameEngineAdapter is null)
                return;
            foreach (var (_, inputBuffer) in _gameEngineAdapter.PlayerInputBuffers)
                inputBuffer.UpdateMinTick(Tick);
        }

        private void SubmitIntermediateScore(int playerIndex, float score, string metadata, DateTime utcTime, TimeSpan gameplayTime) =>
            _gameEngineAdapter?.SubmitIntermediateScore(score, ElympicsPlayer.FromIndex(playerIndex), metadata, utcTime, gameplayTime);

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList, bool reliable) =>
            _gameEngineAdapter?.BroadcastDataToPlayers(rpcMessageList, reliable);

        //TODO refactor this.... ~kpieta 2025-02-27
        internal void SwitchBehaviourToServer()
        {
            _currentPlayer = ServerPlayer;
            _currentIsBot = true;
        }

        internal void SwitchBehaviourToBot(ElympicsPlayer player)
        {
            _currentPlayer = player;
            _currentIsBot = true;
        }

        internal void SwitchBehaviourToClient(ElympicsPlayer player)
        {
            _currentPlayer = player;
            _currentIsBot = false;
        }

        #region IElympics

        public override void EndGame(ResultMatchPlayerDatas? result = null)
        {
            if (_gameEnded)
            {
                _logger.LogWarning("EndGame was called after the game had ended. Ignored.");
                return;
            }
            _endGameRequested = true;
            _matchResult = result;
        }

        public override ElympicsScore Score => Initialized ? _score! : throw new InvalidOperationException("Not initialized");

        #endregion

        private void OnDestroy()
        {
            // Also on an aborted match, where EndGame (the other place disposing it) never runs.
            _snapshotCollector?.Dispose();
            if (_score != null)
                _score.PlayerScoreUpdated -= SubmitIntermediateScore;
            if (_gameEngineAdapter == null)
                return;
            _gameEngineAdapter.PlayerConnected -= OnPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected -= OnPlayerDisconnected;
            _gameEngineAdapter.RpcMessageListReceived -= QueueRpcMessagesToInvoke;
        }
    }
}
