using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    public class ElympicsServer : ElympicsBase
    {
        private static readonly ElympicsPlayer ServerPlayer = ElympicsPlayer.World;

        private ElympicsPlayer _currentPlayer = ElympicsPlayer.Invalid;
        private bool _currentIsBot;
        private bool _currentIsClient;

        public override ElympicsPlayer Player => _currentPlayer;
        public override bool IsServer => true;
        public override bool IsBot => _currentIsBot;
        public override bool IsClient => _currentIsClient;

        private bool _handlingBotsOverride;

        private bool HandlingBotsInServer => _handlingBotsOverride || Config.BotsInServer;
        private bool HandlingClientsInServer { get; set; }

        private GameEngineAdapter _gameEngineAdapter;
        private InitialMatchPlayerDatasGuid _playerData;
        private ElympicsPlayer[] _playersOfBots;
        private ElympicsPlayer[] _playersOfClients;

        private List<ElympicsInput> _inputList;
        private Dictionary<int, TickToPlayerInput> _tickToPlayerInputHolder;

        #region TickAnalysis

        private protected override void TryAttachTickAnalysis()
        {
            TickAnalysis?.Attach(snapshot => elympicsBehavioursManager.ApplySnapshot(snapshot, ignoreTolerance: true), _playerData?.Select(x => x.IsBot).ToArray());
        }

        #endregion TickAnalysis

        internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter, bool handlingBotsOverride = false, bool handlingClientsOverride = false)
        {
            InitializeInternal(elympicsGameConfig);
            _tickToPlayerInputHolder = new Dictionary<int, TickToPlayerInput>();
            SwitchBehaviourToServer();
            _handlingBotsOverride = handlingBotsOverride;
            HandlingClientsInServer = handlingClientsOverride;
            _gameEngineAdapter = gameEngineAdapter;
            Tick = 0;
            _inputList = new List<ElympicsInput>();
            elympicsBehavioursManager.InitializeInternal(this);
            SetupCallbacks();
            if (Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote && !Application.runInBackground)
                Debug.LogError(SdkLogMessages.Error_HalfRemoteWoRunInBacktround);
        }

        private void SetupCallbacks()
        {
            _gameEngineAdapter.PlayerConnected += OnPlayerConnected;
            _gameEngineAdapter.PlayerDisconnected += OnPlayerDisconnected;
            _gameEngineAdapter.ReceivedInitialMatchPlayerDatas += args => Enqueue(() =>
            {
                _playerData = args.Data;
                elympicsBehavioursManager.OnServerInit(args.Data);
                InitializeBotsAndClientInServer(args.Data);
                SetInitialized();
                args.OnInitialized();
            });
            _gameEngineAdapter.RpcMessageListReceived += QueueRpcMessagesToInvoke;
        }

        private void InitializeBotsAndClientInServer(InitialMatchPlayerDatasGuid data)
        {
            if (HandlingBotsInServer)
            {
                var dataOfBots = data.Where(x => x.IsBot).ToList();
                elympicsBehavioursManager.OnBotsOnServerInit(new InitialMatchPlayerDatasGuid(dataOfBots));

                _playersOfBots = dataOfBots.Select(x => x.Player).ToArray();
                CallPlayerConnectedFromBotsOrClients(_playersOfBots);
            }

            if (HandlingClientsInServer)
            {
                var dataOfClients = data.Where(x => !x.IsBot).ToList();
                elympicsBehavioursManager.OnClientsOnServerInit(new InitialMatchPlayerDatasGuid(dataOfClients));

                _playersOfClients = dataOfClients.Select(x => x.Player).ToArray();
                CallPlayerConnectedFromBotsOrClients(_playersOfClients);
            }
        }


        private void CallPlayerConnectedFromBotsOrClients(IEnumerable<ElympicsPlayer> players)
        {
            foreach (var player in players)
                elympicsBehavioursManager.OnPlayerConnected(player);
        }

        protected override bool ShouldDoElympicsUpdate() => Initialized && !(TickAnalysis?.Paused ?? false);

        protected override void ElympicsFixedUpdate()
        {
            using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
            {
                if (HandlingBotsInServer)
                    GatherInputsFromServerBotsOrClient(_playersOfBots, SwitchBehaviourToBot, BotInputGetter);
                if (HandlingClientsInServer)
                    GatherInputsFromServerBotsOrClient(_playersOfClients, SwitchBehaviourToClient, ClientInputGetter);
            }

            _inputList.Clear();
            foreach (var (elympicPlayer, elympicDataWithTickBuffer) in _gameEngineAdapter.PlayerInputBuffers)
            {
                var currentTick = Tick;
                if (elympicDataWithTickBuffer.TryGetDataForTick(currentTick, out var input) || _gameEngineAdapter.LatestSimulatedTickInput.TryGetValue(elympicPlayer, out input))
                {
                    _inputList.Add(input);
                    _gameEngineAdapter.SetLatestSimulatedInputTick(input.Player, input);
                }
            }

            using (ElympicsMarkers.Elympics_ApplyingInputMarker.Auto())
                elympicsBehavioursManager.SetCurrentInputs(_inputList);

            InvokeQueuedRpcMessages();
            elympicsBehavioursManager.CommitVars();

            using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
                elympicsBehavioursManager.ElympicsUpdate();
        }

        private static ElympicsInput ClientInputGetter(ElympicsBehavioursManager manager) => manager.OnInputForClient();
        private static ElympicsInput BotInputGetter(ElympicsBehavioursManager manager) => manager.OnInputForBot();

        private void GatherInputsFromServerBotsOrClient(ElympicsPlayer[] players, Action<ElympicsPlayer> switchElympicsBaseBehaviour, Func<ElympicsBehavioursManager, ElympicsInput> onInput)
        {
            foreach (var playerOfBotOrClient in players)
            {
                switchElympicsBaseBehaviour(playerOfBotOrClient);
                var input = onInput(elympicsBehavioursManager);
                input.Tick = Tick;
                input.Player = playerOfBotOrClient;
                _gameEngineAdapter.AddBotsOrClientsInServerInputToBuffer(input, playerOfBotOrClient);
            }

            SwitchBehaviourToServer();
        }

        protected override void ElympicsLateFixedUpdate()
        {
            using (ElympicsMarkers.Elympics_ProcessSnapshotMarker.Auto())
                if (ShouldSendSnapshot())
                {
                    // gather state info from scene and send to clients
                    PopulateTickToPlayerInputHolder();
                    var snapshots = elympicsBehavioursManager.GetSnapshotsToSend(_tickToPlayerInputHolder, _gameEngineAdapter.Players);
                    AddMetadataToSnapshots(snapshots, TickStartUtc);

                    _gameEngineAdapter.SendSnapshotsToPlayers(snapshots);
                }

            SendQueuedRpcMessages();

            if (TickAnalysis != null)
            {
                var localSnapshotWithInputs = CreateLocalSnapshotWithMetadata();
                localSnapshotWithInputs.TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>(_tickToPlayerInputHolder);
                TickAnalysis.AddSnapshotToAnalysis(localSnapshotWithInputs, null, new ClientTickCalculatorNetworkDetails(Config));
            }

            Tick++;

            foreach (var (_, inputBuffer) in _gameEngineAdapter.PlayerInputBuffers)
                inputBuffer.UpdateMinTick(Tick);
        }

        private void PopulateTickToPlayerInputHolder()
        {
            _tickToPlayerInputHolder.Clear();
            foreach (var (player, inputBufferWithTick) in _gameEngineAdapter.PlayerInputBuffers)
            {
                var tickToPlayerInput = new TickToPlayerInput
                {
                    Data = new Dictionary<long, ElympicsSnapshotPlayerInput>()
                };
                for (var i = inputBufferWithTick.MinTick; i <= inputBufferWithTick.MaxTick; i++)
                    if (inputBufferWithTick.TryGetDataForTick(i, out var elympicsInput))
                    {
                        var snapshotInputData = new ElympicsSnapshotPlayerInput
                        {
                            Data = new List<KeyValuePair<int, byte[]>>(elympicsInput.Data)
                        };
                        tickToPlayerInput.Data.Add(i, snapshotInputData);
                    }
                _tickToPlayerInputHolder[(int)player] = tickToPlayerInput;
            }
        }

        private bool ShouldSendSnapshot() => Tick % Config.SnapshotSendingPeriodInTicks == 0;

        private void AddMetadataToSnapshots(Dictionary<ElympicsPlayer, ElympicsSnapshot> snapshots, DateTime tickStart)
        {
            foreach (var (_, snapshot) in snapshots)
                AddMetadataToSnapshot(tickStart, snapshot);
        }

        private void AddMetadataToSnapshot(DateTime tickStart, ElympicsSnapshot snapshot)
        {
            snapshot.TickStartUtc = tickStart;
            snapshot.Tick = Tick;
        }

        protected override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            _gameEngineAdapter.BroadcastDataToPlayers(rpcMessageList, true);

        private void SwitchBehaviourToServer()
        {
            _currentPlayer = ServerPlayer;
            _currentIsClient = HandlingClientsInServer;
            _currentIsBot = HandlingBotsInServer;
        }

        private void SwitchBehaviourToBot(ElympicsPlayer player)
        {
            _currentPlayer = player;
            _currentIsClient = false;
            _currentIsBot = true;
        }

        private void SwitchBehaviourToClient(ElympicsPlayer player)
        {
            _currentPlayer = player;
            _currentIsClient = true;
            _currentIsBot = false;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && !Application.runInBackground && Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote)
                Debug.LogError(SdkLogMessages.Error_HalfRemoteWoRunInBacktround);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.runInBackground && Config.GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote)
                Debug.LogError(SdkLogMessages.Error_HalfRemoteWoRunInBacktround);
        }

        #region IElympics

        public override void EndGame(ResultMatchPlayerDatas result = null) => _gameEngineAdapter.EndGame(result);

        #endregion
    }
}
