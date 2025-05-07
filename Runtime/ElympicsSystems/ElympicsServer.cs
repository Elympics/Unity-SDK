#nullable enable
using System.Collections.Generic;
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

        private bool _handlingBotsOverride;

        private bool HandlingBotsInServer => _handlingBotsOverride || Config.BotsInServer;
        private bool HandlingClientsInServer { get; set; }

        private bool _isReplay;

        private bool _endGameRequested;
        private ResultMatchPlayerDatas _matchResult;
        private GameEngineAdapter _gameEngineAdapter;
        private InitialMatchPlayerDatasGuid _playerData;

        private List<ElympicsInput> _inputList;

        private SnapshotAnalysisCollector _snapshotCollector = null!;
        private IServerPlayerHandler _serverPlayerHandler = null!;
        private IServerElympicsUpdateLoop _serverElympicsUpdate = null!;
        private ElympicsSnapshotWithMetadata _currentSnapshot = null!;
        private ElympicsSnapshotWithMetadata? _previousSnapshot;

        internal void InitializeInternal(
            ElympicsGameConfig elympicsGameConfig,
            GameEngineAdapter gameEngineAdapter,
            ElympicsBehavioursManager elympicsBehavioursManager,
            IServerPlayerHandler playerHandler,
            SnapshotAnalysisCollector snapshotAnalysisCollector,
            IServerElympicsUpdateLoop serverElympicsUpdate,
            bool handlingBotsOverride = false,
            bool handlingClientsOverride = false)
        {
            _serverPlayerHandler = playerHandler;
            _snapshotCollector = snapshotAnalysisCollector;
            _serverElympicsUpdate = serverElympicsUpdate;
            _isReplay = _serverElympicsUpdate is SnapshotReplayElympicsUpdateLoop;
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            SwitchBehaviourToServer();
            _handlingBotsOverride = handlingBotsOverride;
            HandlingClientsInServer = handlingClientsOverride;
            _gameEngineAdapter = gameEngineAdapter;
            _inputList = new List<ElympicsInput>();
            SetupCallbacks();
            LogHalfRemoteRunInBackgroundErrorIfApplicable();
        }

        private void SetupCallbacks()
        {
            _gameEngineAdapter.PlayerConnected += OnPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected += OnPlayerDisconnected;
            _gameEngineAdapter.ReceivedInitialMatchPlayerDatas += args => Enqueue(() =>
            {
                _playerData = args.Data;
                _snapshotCollector.Initialize(
                    Config,
                    _playerData,
                    new LatestMessagePackSerializer()
                );
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

            using (ElympicsMarkers.Elympics_SnapshotCollector.Auto())
                _snapshotCollector.CaptureSnapshot(_previousSnapshot, _currentSnapshot);
            _previousSnapshot = _currentSnapshot;
        }

        protected override void ElympicsRenderUpdate(in RenderData renderData) => _serverElympicsUpdate.HandleRenderFrame(renderData);

        protected override void ElympicsLateFixedUpdate()
        {
            SendQueuedRpcMessages();
            _serverElympicsUpdate.FinalizeTick(_currentSnapshot);
            if (_endGameRequested)
            {
                _endGameRequested = false;
                SetDeInitialized();
                _snapshotCollector.Dispose();
                _gameEngineAdapter.EndGame(_matchResult);
            }

            foreach (var (_, inputBuffer) in _gameEngineAdapter.PlayerInputBuffers)
                inputBuffer.UpdateMinTick(Tick);
        }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            _gameEngineAdapter.BroadcastDataToPlayers(rpcMessageList, true);


        //TODO refactor this....
        internal void SwitchBehaviourToServer()
        {
            _currentPlayer = ServerPlayer;
            _currentIsBot = HandlingBotsInServer;
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

        private void LogHalfRemoteRunInBackgroundErrorIfApplicable()
        {
            if (!Application.runInBackground && Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote)
                ElympicsLogger.LogError("Development Mode is set to Half Remote "
                    + "but PlayerSettings \"Run In Background\" option is false, "
                    + "hence network simulation will not be performed in out-of-focus windows. "
                    + "Please make sure that PlayerSettings \"Run In Background\" option is set to true.");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                LogHalfRemoteRunInBackgroundErrorIfApplicable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                LogHalfRemoteRunInBackgroundErrorIfApplicable();
        }

        #region IElympics

        public override void EndGame(ResultMatchPlayerDatas result = null)
        {
            _endGameRequested = true;
            _matchResult = result;
        }

        #endregion

        private void OnDestroy() => _snapshotCollector?.Dispose();
    }
}
