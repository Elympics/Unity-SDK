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

        private long? _gameStartTick;
        private DateTime? _gameStartUtc;
        private bool _clientsInServerReady;
        private long? _rejoinDeadlineTick;
        private bool _preStartRpcIgnored;

        internal DateTime InitializedUtc { get; set; }
        internal DateTime? FirstHumanConnectedUtc { get; set; }
        internal HashSet<ElympicsPlayer> HumanPlayers { get; } = new();
        internal HashSet<ElympicsPlayer> ReadyPlayers { get; } = new();
        internal HashSet<ElympicsPlayer> ConnectedHumans { get; } = new();

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
            _gameEngineAdapter.PlayerReadyReceived += OnPlayerReadyReceived;
            _gameEngineAdapter.ReceivedInitialMatchPlayerDatas += args => Enqueue(() =>
            {
                _playerData = args.Data;
                CreateScore(args.Data.Count);
                HumanPlayers.UnionWith(args.Data.Where(data => !data.IsBot).Select(data => data.Player));
                InitializedUtc = DateTime.UtcNow;
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
                UpdateGameStart();

            var snapshot = _serverElympicsUpdate.GenerateSnapshot();
            if (_isReplay)
                _gameStartTick = snapshot.GameStartTick;
            else
                TerminateIfPlayersLeft();
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
            _score?.SubmitIfEnabled();
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

        private void UpdateGameStart()
        {
            if (_gameStartTick.HasValue || _endGameRequested)
                return;

            if (_clientsInServerReady || ReadyPlayers.Count == HumanPlayers.Count)
                ScheduleGameStart();
            else if (FirstHumanConnectedUtc is { } firstConnectedUtc)
                EndGameIfReadyTimedOut(firstConnectedUtc);
            else
                EndGameIfNoPlayerTimedOut();
        }

        private void ScheduleGameStart()
        {
            _gameStartTick = Tick + Config.GameStartDelayTicks;
            _logger.LogInfo($"All players are ready. The game starts at tick {_gameStartTick} (current tick: {Tick}).");
        }

        private void EndGameIfNoPlayerTimedOut()
        {
            if (!HasElapsed(InitializedUtc, Config.NoPlayerTimeoutSeconds))
                return;
            _logger.LogError($"Ending the match without a result, because no player connected within {Config.NoPlayerTimeoutSeconds} s "
                + $"(see {nameof(ElympicsGameConfig)}.{nameof(ElympicsGameConfig.NoPlayerTimeoutSeconds)}).");
            EndGame();
        }

        private void EndGameIfReadyTimedOut(DateTime firstConnectedUtc)
        {
            if (!HasElapsed(firstConnectedUtc, Config.ReadyTimeoutSeconds))
                return;
            var notReady = string.Join(", ", HumanPlayers.Where(player => !ReadyPlayers.Contains(player)));
            _logger.LogError($"Ending the match without a result, because not all players were ready within {Config.ReadyTimeoutSeconds} s from the first connection. "
                + $"Not ready: {notReady}. Fix: call {nameof(IElympics)}.{nameof(MarkReady)}() on every player client from a player action.");
            EndGame();
        }

        private static bool HasElapsed(DateTime sinceUtc, float seconds) => (DateTime.UtcNow - sinceUtc).TotalSeconds >= seconds;

        private protected override void OnGameStarted(long startTick)
        {
            _logger.LogInfo($"Game started at tick {startTick}.");
            var gameStartUtc = TickStartUtc - TimeSpan.FromSeconds((Tick - startTick) * (double)TickDuration);
            _gameStartUtc = gameStartUtc;
            _score?.Start(gameStartUtc);
            base.OnGameStarted(startTick);
        }

        internal override bool ShouldIgnorePlayerRpc(ElympicsRpcMessage rpcMessage) =>
            rpcMessage.Sender >= 0 && ShouldIgnoreRpcSentBeforeGameStart(rpcMessage);

        // In local mode, player RPCs sent outside input gathering carry the World as their sender.
        internal override bool ShouldIgnoreLocalPlayerRpc(ElympicsRpcMessage rpcMessage) => ShouldIgnoreRpcSentBeforeGameStart(rpcMessage);

        private bool ShouldIgnoreRpcSentBeforeGameStart(ElympicsRpcMessage rpcMessage)
        {
            if (IsGameStartedAt(rpcMessage.SentOnTick))
                return false;
            if (!_preStartRpcIgnored)
            {
                _preStartRpcIgnored = true;
                _logger.LogWarning($"Ignoring player RPCs sent before the game start (first: player {ElympicsPlayer.FromIndexExtended(rpcMessage.Sender)}, tick {rpcMessage.SentOnTick}). "
                    + $"Check {nameof(IElympics)}.{nameof(IElympics.IsGameStarted)} before calling them.");
            }
            return true;
        }

        internal ResultMatchPlayerDatas? ResolveMatchResult(ResultMatchPlayerDatas? result)
        {
            if (!GameStartReached)
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

        private void OnPlayerReadyReceived(ElympicsPlayer player) => Enqueue(() =>
        {
            if (HumanPlayers.Contains(player) && ReadyPlayers.Add(player))
                _logger.LogInfo($"Player {player} is ready.");
        });

        private void OnPlayerConnected(ElympicsPlayer player) => Enqueue(() =>
        {
            ElympicsBehavioursManager.OnPlayerConnected(player);
            if (!HumanPlayers.Contains(player))
                return;
            _ = ConnectedHumans.Add(player);
            FirstHumanConnectedUtc ??= DateTime.UtcNow;
        });

        private void OnPlayerDisconnected(ElympicsPlayer player) => Enqueue(() =>
        {
            ElympicsBehavioursManager.OnPlayerDisconnected(player);
            _ = ConnectedHumans.Remove(player);
        });

        /// <summary>Ends the match once players have left (see <see cref="ElympicsGameConfig.TerminateWhenPlayersLeave"/>) and haven't rejoined in time.</summary>
        private void TerminateIfPlayersLeft()
        {
            if (HandlingClientsInServer || !GameStartReached || _endGameRequested)
                return;

            var havePlayersLeft = HumanPlayers.Count > 0 && Config.TerminateWhenPlayersLeave switch
            {
                TerminationOption.Any => ConnectedHumans.Count < HumanPlayers.Count,
                TerminationOption.All => ConnectedHumans.Count == 0,
                _ => false,
            };

            if (havePlayersLeft)
                EndGameIfRejoinTimedOut();
            else
                CancelRejoinDeadline();
        }

        private void EndGameIfRejoinTimedOut()
        {
            if (_rejoinDeadlineTick is null)
            {
                _rejoinDeadlineTick = Tick + Config.RejoinTimeoutTicks;
                if (Config.RejoinTimeoutTicks > 0)
                    _logger.LogWarning($"{Config.TerminateWhenPlayersLeave} players left. The match ends in {Config.RejoinTimeoutSeconds} s unless they rejoin.");
            }
            if (Tick < _rejoinDeadlineTick)
                return;
            _logger.LogWarning($"Ending the match, because {Config.TerminateWhenPlayersLeave} players left "
                + $"(see {nameof(ElympicsGameConfig)}.{nameof(ElympicsGameConfig.TerminateWhenPlayersLeave)}).");
            EndGame();
        }

        private void CancelRejoinDeadline()
        {
            if (_rejoinDeadlineTick is null)
                return;
            _rejoinDeadlineTick = null;
            _logger.LogInfo("Players rejoined, so the match goes on.");
        }

        private static void LogMatchData(InitialMatchPlayerDatasGuid data)
        {
            var humans = data.Count(x => !x.IsBot);
            var sb = new StringBuilder()
                .AppendLine($"Game initialized for {data.Count} players (including {data.Count - humans} bots).");
            foreach (var playerData in data)
                _ = sb.AppendLine($"Player {playerData.UserId} {(playerData.IsBot ? "Bot" : "Human")} room {playerData.RoomId} teamIndex {playerData.TeamIndex}");
            ElympicsLogger.LogInfo(sb.ToString());
        }

        private protected void CreateScore(int playerCount)
        {
            _score = new ElympicsScore(playerCount, GetGameplayTime);
            _score.PlayerScoreUpdated += SubmitIntermediateScore;
        }

        private TimeSpan GetGameplayTime(int playerIndex, DateTime currentTime) =>
            currentTime - (GameStartUtc ?? throw new InvalidOperationException("The game has not started yet"));

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

        public override DateTime? GameStartUtc => _gameStartUtc;

        public override long? GameStartTick => _gameStartTick;

        /// <remarks>Only in local modes, where the server simulates the clients: marks all players ready, even before server init. Otherwise a no-op.</remarks>
        public override void MarkReady()
        {
            if (HandlingClientsInServer)
                _clientsInServerReady = true;
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
            _gameEngineAdapter.PlayerReadyReceived -= OnPlayerReadyReceived;
            _gameEngineAdapter.RpcMessageListReceived -= QueueRpcMessagesToInvoke;
        }
    }
}
