#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private long? _startTick;
        private DateTime _initializedUtc;
        private ElympicsPlayer[] _humanPlayers = Array.Empty<ElympicsPlayer>();
        private readonly HashSet<ElympicsPlayer> _readyPlayers = new();
        private readonly HashSet<ElympicsPlayer> _connectedHumans = new();

        internal void InitializeInternal(
            ElympicsGameConfig elympicsGameConfig,
            GameEngineAdapter gameEngineAdapter,
            ElympicsBehavioursManager elympicsBehavioursManager,
            IServerPlayerHandler playerHandler,
            SnapshotAnalysisCollector snapshotAnalysisCollector,
            IServerElympicsUpdateLoop serverElympicsUpdate,
            bool handlingClientsOverride = false)
        {
            _serverPlayerHandler = playerHandler;
            _snapshotCollector = snapshotAnalysisCollector;
            _serverElympicsUpdate = serverElympicsUpdate;
            _isReplay = _serverElympicsUpdate is SnapshotReplayElympicsUpdateLoop;
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            CreateScore(elympicsGameConfig.MaxPlayers);
            SwitchBehaviourToServer();
            HandlingClientsInServer = handlingClientsOverride;
            _gameEngineAdapter = gameEngineAdapter;
            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            if (_gameEngineAdapter is null)
                return;
            _gameEngineAdapter.PlayerConnected += OnPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected += OnPlayerDisconnected;
            _gameEngineAdapter.PlayerConnected += TrackPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected += TrackPlayerDisconnected;
            _gameEngineAdapter.PlayerReadyReceived += OnPlayerReadyReceived;
            _gameEngineAdapter.ReceivedInitialMatchPlayerDatas += args => Enqueue(() =>
            {
                _playerData = args.Data;
                // Sized from the init data, so that the score fallback result matches the players in the match.
                CreateScore(args.Data.Count);
                _humanPlayers = args.Data.Where(data => !data.IsBot).Select(data => data.Player).ToArray();
                _initializedUtc = DateTime.UtcNow;
                if (!_isReplay)
                    LogMatchData(args.Data);
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

            if (!_isReplay)
                UpdateMatchStart();

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
            _score?.SubmitIfStarted();
            _serverElympicsUpdate.FinalizeTick(_currentSnapshot);
            if (_endGameRequested)
            {
                _endGameRequested = false;
                _gameEnded = true;
                SetDeInitialized();
                _snapshotCollector?.Dispose();
                _gameEngineAdapter?.EndGame(ResolveMatchResult(_matchResult));
            }

            if (_gameEngineAdapter is null)
                return;
            foreach (var (_, inputBuffer) in _gameEngineAdapter.PlayerInputBuffers)
                inputBuffer.UpdateMinTick(Tick);
        }

        private void UpdateMatchStart()
        {
            if (_startTick.HasValue || _endGameRequested || _gameEnded)
                return;

            if (Array.TrueForAll(_humanPlayers, _readyPlayers.Contains))
            {
                _startTick = Tick + Config.MatchStartDelayTicks;
                _logger.LogInfo($"All players are ready. The match starts at tick {_startTick} (current tick: {Tick}).");
                return;
            }

            if ((DateTime.UtcNow - _initializedUtc).TotalSeconds < Config.ReadyTimeoutSeconds)
                return;
            var notReady = string.Join(", ", _humanPlayers.Where(player => !_readyPlayers.Contains(player)));
            _logger.LogError($"Ending the match without a result, because not all players were ready within {Config.ReadyTimeoutSeconds} s. "
                + $"Not ready: {notReady}. Fix: call {nameof(IElympics)}.{nameof(MarkReady)}() on every player client once the game is playable.");
            EndGame();
        }

        private protected override void OnMatchStarted(long startTick)
        {
            _logger.LogInfo($"Match started at tick {startTick}.");
            _score?.Start(TickStartUtc);
            TerminateIfPlayersLeft();
        }

        private ResultMatchPlayerDatas? ResolveMatchResult(ResultMatchPlayerDatas? result)
        {
            if (!MatchStartReached)
            {
                if (result != null)
                    _logger.LogWarning("The match result was ignored, because the match ended before it started. Sending no result.");
                return null;
            }
            if (result != null || _score == null)
                return result;

            result = new ResultMatchPlayerDatas();
            for (var i = 0; i < _playerData!.Count; i++)
                result.Add(new ResultMatchPlayerData { MatchmakerData = new[] { _score.GetScore(i) }, GameEngineData = Array.Empty<byte>() });
            return result;
        }

        private void OnPlayerReadyReceived(ElympicsPlayer player) => Enqueue(() => MarkPlayerReady(player));

        private void MarkPlayerReady(ElympicsPlayer player)
        {
            if (Array.IndexOf(_humanPlayers, player) >= 0 && _readyPlayers.Add(player))
                _logger.LogInfo($"Player {player} is ready.");
        }

        private void TrackPlayerConnected(ElympicsPlayer player) => Enqueue(() =>
        {
            if (Array.IndexOf(_humanPlayers, player) >= 0)
                _ = _connectedHumans.Add(player);
        });

        private void TrackPlayerDisconnected(ElympicsPlayer player) => Enqueue(() =>
        {
            _ = _connectedHumans.Remove(player);
            TerminateIfPlayersLeft();
        });

        private void TerminateIfPlayersLeft()
        {
            if (HandlingClientsInServer || _isReplay || !MatchStartReached || _endGameRequested || _gameEnded || _humanPlayers.Length == 0)
                return;
            var shouldTerminate = Config.TerminateWhenPlayersLeave switch
            {
                TerminationOption.Any => _connectedHumans.Count < _humanPlayers.Length,
                TerminationOption.All => _connectedHumans.Count == 0,
                _ => false,
            };
            if (!shouldTerminate)
                return;
            _logger.LogWarning($"Ending the match, because {Config.TerminateWhenPlayersLeave} players left "
                + $"(see {nameof(ElympicsGameConfig)}.{nameof(ElympicsGameConfig.TerminateWhenPlayersLeave)}).");
            EndGame();
        }

        private static void LogMatchData(InitialMatchPlayerDatasGuid data)
        {
            var humans = data.Count(x => !x.IsBot);
            var sb = new StringBuilder()
                .AppendLine($"Game initialized for {data.Count} players (including {data.Count - humans} bots).")
                .AppendLine($"MatchId: {data.MatchId}")
                .AppendLine($"QueueName: {data.QueueName}")
                .AppendLine($"RegionName: {data.RegionName}")
                .AppendLine($"CustomMatchmakingData: {data.CustomMatchmakingData?.Count.ToString() ?? "null"}")
                .AppendLine($"CustomRoomData: {(data.CustomRoomData != null ? string.Join(", ", data.CustomRoomData.Select(x => x.Key)) : "null")}")
                .AppendLine($"ExternalGameData: {data.ExternalGameData?.Length.ToString() ?? "null"}");
            foreach (var playerData in data)
                _ = sb.AppendLine($"Player {playerData.UserId} {(playerData.IsBot ? "Bot" : "Human")} room {playerData.RoomId} teamIndex {playerData.TeamIndex}");
            ElympicsLogger.LogInfo(sb.ToString());
        }

        private void CreateScore(int playerCount)
        {
            if (_score != null)
                _score.PlayerScoreUpdated -= SubmitIntermediateScore;
            _score = new ElympicsScore(playerCount);
            _score.PlayerScoreUpdated += SubmitIntermediateScore;
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

        public override long? StartTick => _startTick;

        /// <remarks>In local modes, the server simulates the clients, so this marks all players ready. Otherwise, it does nothing.</remarks>
        public override void MarkReady()
        {
            if (!HandlingClientsInServer)
                return;
            foreach (var player in _humanPlayers)
                MarkPlayerReady(player);
        }

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
            _gameEngineAdapter.PlayerConnected -= TrackPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected -= TrackPlayerDisconnected;
            _gameEngineAdapter.PlayerReadyReceived -= OnPlayerReadyReceived;
            _gameEngineAdapter.RpcMessageListReceived -= QueueRpcMessagesToInvoke;
        }
    }
}
