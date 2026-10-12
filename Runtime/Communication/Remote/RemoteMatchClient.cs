using System;
using System.Collections.Generic;
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
        private readonly RingBufferElympicsDataWithTick<ElympicsInput> _input;

        private readonly ElympicsInput[] _inputsBuffer;
        private readonly List<ElympicsInput> _inputsToSend;

        private long _lastReceivedSnapshotTick;

        public RemoteMatchClient(IGameServerClient gameServerClient, ElympicsGameConfig config)
        {
            _gameServerClient = gameServerClient;
            _input = new RingBufferElympicsDataWithTick<ElympicsInput>(config.InputsToSendBufferSize);
            _inputsBuffer = new ElympicsInput[config.InputsToSendBufferSize];
            _inputsToSend = new List<ElympicsInput>(config.InputsToSendBufferSize);

            gameServerClient.Synchronized += OnSynchronized;
            gameServerClient.InGameDataReceived += ProcessReceivedInGameData;
        }

        private void OnSynchronized(TimeSynchronizationData data) => Synchronized?.Invoke(data);

        private void ProcessReceivedInGameData(string label, InGameDataMessage message)
        {
            var deserializedData = MessagePackSerializer.Deserialize<IFromServer>(Convert.FromBase64String(message.Data));
            switch (deserializedData)
            {
                case ElympicsSnapshot snapshot:
                {
                    _input.UpdateMinTick(snapshot.Tick + 1);
                    SnapshotReceived?.Invoke(snapshot);
                    break;
                }
                case ElympicsRpcMessageList rpcMessageList:
                    RpcMessageListReceived?.Invoke(rpcMessageList);
                    break;
                // ReSharper disable once RedundantEmptySwitchSection
                default:
                    //Do Nothing.
                    break;
            }
        }

        public void AddInputToSendBuffer(ElympicsInput input) => _ = _input.TryAddData(input);

        private void GetInputCollectionToSend()
        {
            Array.Clear(_inputsBuffer, 0, _inputsBuffer.Length);
            _inputsToSend.Clear();
            _input.GetInputListNonAlloc(_inputsBuffer, out var collectionSize);

            for (var i = 0; i < collectionSize; i++)
                _inputsToSend.Add(_inputsBuffer[i]);
        }

        public void SetLastReceivedSnapshot(long tick) => _lastReceivedSnapshotTick = tick;

        public void SendBufferInput(long tick)
        {
            if (_input.Count() <= 0)
                return;
            GetInputCollectionToSend();
            SendDataToServer(new ElympicsInputList { Values = _inputsToSend, LastReceivedSnapshot = _lastReceivedSnapshotTick }, false);
        }

        public void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList, bool reliable) =>
            SendDataToServer(rpcMessageList, reliable);

        private void SendDataToServer(IToServer data, bool reliable)
        {
            var dataSerialized = MessagePackSerializer.Serialize(data);
            Action<byte[]> sendDataAsync = reliable ? _gameServerClient.SendInGameDataReliable : _gameServerClient.SendInGameDataUnreliable;
            sendDataAsync(dataSerialized);
        }
        public void Dispose() => _input?.Dispose();
    }
}
