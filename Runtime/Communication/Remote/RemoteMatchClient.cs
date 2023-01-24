using System;
using System.Threading.Tasks;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace Elympics
{
	public class RemoteMatchClient : IMatchClient
	{
		public event Action<TimeSynchronizationData> Synchronized;
		public event Action<ElympicsSnapshot>        SnapshotReceived;

		private readonly IGameServerClient  _gameServerClient;
		private readonly RingBuffer<byte[]> _inputRingBuffer;

		public RemoteMatchClient(IGameServerClient gameServerClient, ElympicsGameConfig config)
		{
			_gameServerClient = gameServerClient;
			_inputRingBuffer = new RingBuffer<byte[]>(config.InputsToSendBufferSize);

			gameServerClient.Synchronized += OnSynchronized;
			gameServerClient.InGameDataReliableReceived += OnInGameDataReliableReceived;
			gameServerClient.InGameDataUnreliableReceived += OnInGameDataUnreliableReceived;
		}

		private void OnSynchronized(TimeSynchronizationData data) => Synchronized?.Invoke(data);

		private void OnInGameDataReliableReceived(InGameDataMessage message)
		{
			var snapshot = Convert.FromBase64String(message.Data).Deserialize<ElympicsSnapshot>();
			SnapshotReceived?.Invoke(snapshot);
		}

		private void OnInGameDataUnreliableReceived(InGameDataMessage message)
		{
			var snapshot = Convert.FromBase64String(message.Data).Deserialize<ElympicsSnapshot>();
			SnapshotReceived?.Invoke(snapshot);
		}

		public async Task SendInputReliable(ElympicsInput input)
		{
			var inputSerialized = input.Serialize();
			await _gameServerClient.SendInGameDataReliableAsync(inputSerialized);
		}

		public async Task SendInputUnreliable(ElympicsInput input)
		{
			_inputRingBuffer.PushBack(input.Serialize());
			var serializedInputs = _inputRingBuffer.ToArray().MergeBytePackage();
			await _gameServerClient.SendInGameDataUnreliableAsync(serializedInputs);
		}
	}
}
