using System;

namespace Elympics
{
    public class ElympicsBot : ElympicsBase
    {
        private const int SafeInputLagForBotMs = 50;

        public override ElympicsPlayer Player => _gameBotAdapter.Player;
        public override bool IsBot => true;
        public override long Tick => 0;

        private static readonly object LastReceivedSnapshotLock = new();
        private ElympicsSnapshot _lastReceivedSnapshot;
        private bool _started;

        private GameBotAdapter _gameBotAdapter;


        internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter, ElympicsBehavioursManager elympicsBehavioursManager)
        {
            InitializeInternal(elympicsGameConfig, elympicsBehavioursManager);
            _gameBotAdapter = gameBotAdapter;
            _gameBotAdapter.SnapshotReceived += OnSnapshotReceived;
            _gameBotAdapter.RpcMessageListReceived += QueueRpcMessagesToInvoke;
            _gameBotAdapter.InitializedWithMatchPlayerData += data =>
            {
                OnStandaloneBotInit(data);
                Enqueue(SetInitialized);
            };
        }

        private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
        {
            if (!_started)
                StartBot();

            lock (LastReceivedSnapshotLock)
                if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
                    _lastReceivedSnapshot = elympicsSnapshot;
        }

        private void StartBot() => _started = true;

        protected override bool ShouldDoElympicsUpdate() => Initialized && _started;

        internal override void ElympicsFixedUpdate()
        {
            ElympicsSnapshot snapshot;
            lock (LastReceivedSnapshotLock)
                snapshot = _lastReceivedSnapshot;
            ElympicsBehavioursManager.ApplySnapshot(snapshot);
            ProcessInput(snapshot.Tick);
            InvokeQueuedRpcMessages();
            ElympicsBehavioursManager.CommitVars();
            ElympicsBehavioursManager.ElympicsUpdate();
            SendQueuedRpcMessages();
        }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            _gameBotAdapter.SendRpcMessageList(rpcMessageList);

        private void ProcessInput(long snapshotTick)
        {
            var input = CollectRawInput();
            AddMetadataToInput(input, snapshotTick);
            SendInput(input);
        }

        private ElympicsInput CollectRawInput() => ElympicsBehavioursManager.OnInputForBot();

        private void AddMetadataToInput(ElympicsInput input, long snapshotTick)
        {
            input.Tick = (int)(snapshotTick + Math.Max(1, SafeInputLagForBotMs * Config.TicksPerSecond / 1000.0) + Config.InputLagTicks);
            input.Player = Player;
        }

        private void SendInput(ElympicsInput input) => _gameBotAdapter.SendInput(input);
    }
}
