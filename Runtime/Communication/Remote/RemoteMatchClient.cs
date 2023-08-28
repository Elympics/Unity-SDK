using System;
using System.Threading.Tasks;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;
using MessagePack;

namespace Elympics
{
    public class RemoteMatchClient : IMatchClient
    {
        public event Action<TimeSynchronizationData> Synchronized;
        public event Action<ElympicsSnapshot> SnapshotReceived;
        public event Action<ElympicsRpcMessageList> RpcMessageListReceived;

        private readonly IGameServerClient _gameServerClient;
        private readonly RingBuffer<ElympicsInput> _inputRingBuffer;

        public RemoteMatchClient(IGameServerClient gameServerClient, ElympicsGameConfig config)
        {
            _gameServerClient = gameServerClient;
            _inputRingBuffer = new RingBuffer<ElympicsInput>(config.InputsToSendBufferSize);

            gameServerClient.Synchronized += OnSynchronized;
            gameServerClient.InGameDataReliableReceived += ProcessReceivedInGameData;
            gameServerClient.InGameDataUnreliableReceived += ProcessReceivedInGameData;
        }

        private void OnSynchronized(TimeSynchronizationData data) => Synchronized?.Invoke(data);

        private void ProcessReceivedInGameData(InGameDataMessage message)
        {
            var deserializedData = MessagePackSerializer.Deserialize<IFromServer>(Convert.FromBase64String(message.Data));
            if (deserializedData is ElympicsSnapshot snapshot)
                SnapshotReceived?.Invoke(snapshot);
            else if (deserializedData is ElympicsRpcMessageList rpcMessageList)
                RpcMessageListReceived?.Invoke(rpcMessageList);
        }

        public async Task SendInput(ElympicsInput input)
        {
            _inputRingBuffer.PushBack(input);
            await SendDataToServer(new ElympicsInputList { Values = _inputRingBuffer.ToList() }, false);
        }

        public async Task SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            await SendDataToServer(rpcMessageList, true);

        private async Task SendDataToServer(IToServer data, bool reliable)
        {
            var dataSerialized = MessagePackSerializer.Serialize(data);
            Func<byte[], Task> sendDataAsync = reliable
                ? _gameServerClient.SendInGameDataReliableAsync
                : _gameServerClient.SendInGameDataUnreliableAsync;
            await sendDataAsync(dataSerialized);
        }
    }
}
