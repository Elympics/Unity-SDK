using System;

namespace Elympics
{
	public class ElympicsBot : ElympicsBase
	{
		private const int SafeInputLagForBotMs = 50;

		public override ElympicsPlayer Player => _gameBotAdapter.Player;
		public override bool           IsBot    => true;

		private ElympicsSnapshot _lastReceivedSnapshot;
		private bool             _started;

		private GameBotAdapter _gameBotAdapter;


		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter)
		{
			base.InitializeInternal(elympicsGameConfig);
			_gameBotAdapter = gameBotAdapter;
			elympicsBehavioursManager.InitializeInternal(this);
			_gameBotAdapter.SnapshotReceived += OnSnapshotReceived;
			_gameBotAdapter.InitializedWithMatchPlayerData += OnStandaloneBotInit;
		}

		private void OnSnapshotReceived(ElympicsSnapshot elympicsSnapshot)
		{
			if (!_started) StartBot();

			if (_lastReceivedSnapshot == null || _lastReceivedSnapshot.Tick < elympicsSnapshot.Tick)
				_lastReceivedSnapshot = elympicsSnapshot;
		}

		private void StartBot() => _started = true;

		protected override bool ShouldDoFixedUpdate() => _gameBotAdapter.Initialized && _started;

		protected override void DoFixedUpdate()
		{
			var snapshot = _lastReceivedSnapshot;
			elympicsBehavioursManager.ApplySnapshot(snapshot);
			ProcessInput(snapshot.Tick);
		}

		private void ProcessInput(long snapshotTick)
		{
			var input = CollectRawInput();
			AddMetadataToInput(input, snapshotTick);
			SendInput(input);
		}

		private ElympicsInput CollectRawInput() => elympicsBehavioursManager.OnInputForBot();

		private void AddMetadataToInput(ElympicsInput input, long snapshotTick)
		{
			input.Tick = (int) (snapshotTick + Math.Max(1, SafeInputLagForBotMs * Config.TicksPerSecond / 1000.0) + Config.InputLagTicks);
			input.Player = Player;
		}

		private void SendInput(ElympicsInput input) => _gameBotAdapter.SendInputUnreliable(input);
	}
}
