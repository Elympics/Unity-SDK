using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using MessagePack;
using UnityConnectors.HalfRemote;
using UnityConnectors.HalfRemote.Ntp;
using UnityEngine;
using Random = System.Random;

namespace Elympics
{
    public class HalfRemoteMatchClientAdapter : IMatchClient
    {
        private const int MaxJitterMultiplier = 3;

        public event Action<TimeSynchronizationData> Synchronized;
        public event Action<ElympicsSnapshot> SnapshotReceived;
        public event Action<ElympicsRpcMessageList> RpcMessageListReceived;
        public event Action<byte[]> InGameDataUnreliableReceived;

        private readonly RingBufferElympicsDataWithTick<ElympicsInput> _input;
        private readonly ElympicsInput[] _inputsBuffer;
        private readonly List<ElympicsInput> _inputsToSend;

        private readonly WaitForSeconds _synchronizationDelay;
        private readonly HalfRemoteLagConfig _lagConfig;
        private readonly Random _lagRandom;

        private string _userId;
        private HalfRemoteMatchClient _client;
        private bool _playerDisconnected;
        private long _lastReceivedSnapshot;

        public HalfRemoteMatchClientAdapter(ElympicsGameConfig config)
        {
            _input = new RingBufferElympicsDataWithTick<ElympicsInput>(config.InputsToSendBufferSize);
            _inputsBuffer = new ElympicsInput[config.InputsToSendBufferSize];
            _inputsToSend = new List<ElympicsInput>(config.InputsToSendBufferSize);
            _lagConfig = config.HalfRemoteLagConfig;
            _lagRandom = new Random(config.HalfRemoteLagConfig.RandomSeed);
        }

        internal IEnumerator ConnectToServer(Action<bool> connectedCallback, string userId, HalfRemoteMatchClient client)
        {
            _userId = userId;

            _client = client;
            _client.ReliableReceivingError += ElympicsLogger.LogError;
            _client.ReliableReceivingEnded += () =>
            {
                _playerDisconnected = true;
                ElympicsLogger.Log("Reliable receiving ended.");
            };
            _client.UnreliableReceivingError += ElympicsLogger.LogError;
            _client.UnreliableReceivingEnded += () => ElympicsLogger.Log("Unreliable receiving ended.");
            _client.WebRtcUpgraded += () => ElympicsLogger.Log("Upgraded connection to WebRTC.");
            _client.NtpReceived += OnNtpReceived;
            _client.InGameDataForPlayerOnReliableChannelGenerated += OnReliableInGameDataReceived;
            _client.InGameDataForPlayerOnUnreliableChannelGenerated += OnUnreliableInGameDataReceived;

            ElympicsLogger.Log("Connected to a half remote server.");
            connectedCallback?.Invoke(true);

            return Synchronization();
        }

        public void SetLastReceivedSnapshot(long tick) => _lastReceivedSnapshot = tick;

        public void PlayerConnected() => _client.PlayerConnected();
        public void PlayerDisconnected() => _client?.PlayerDisconnected();

        public void AddInputToSendBuffer(ElympicsInput input) => _ = _input.TryAddData(input);
        public async Task SendBufferInput(long tick)
        {
            if (_input.Count() > 0)
            {
                GetInputCollectionToSend();
                var data = new ElympicsInputList { Values = _inputsToSend, LastReceivedSnapshot = _lastReceivedSnapshot };
                await SendRawDataToServer(MessagePackSerializer.Serialize<IToServer>(data), false);
            }
        }

        private void GetInputCollectionToSend()
        {
            Array.Clear(_inputsBuffer, 0, _inputsBuffer.Length);
            _inputsToSend.Clear();
            _input.GetInputListNonAlloc(_inputsBuffer, out var collectionSize);

            for (var i = 0; i < collectionSize; i++)
                _inputsToSend.Add(_inputsBuffer[i]);
        }

        public async Task SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            await SendRawDataToServer(MessagePackSerializer.Serialize<IToServer>(rpcMessageList), true);

        public async Task SendRawDataToServer(byte[] rawData, bool reliable)
        {
            Action<byte[]> sendDataAsync = reliable ? _client.SendInputReliable : _client.SendInputUnreliable;

            await RunWithLag(() => sendDataAsync(rawData));
        }

        private void OnUnreliableInGameDataReceived(byte[] data, string userId)
        {
            if (userId != _userId)
                return;
            InGameDataUnreliableReceived?.Invoke(data);
            ProcessDataFromServer(data);
        }

        private void OnReliableInGameDataReceived(byte[] data, string userId)
        {
            if (userId != _userId)
                return;
            ProcessDataFromServer(data);
        }

        private void ProcessDataFromServer(byte[] serializedData)
        {
            var deserializedData = MessagePackSerializer.Deserialize<IFromServer>(serializedData);
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
                    //Do nothing.
                    break;
            }
        }

        private async UniTask RunWithLag(Action action)
        {
            GetNewLag(out var lost, out var lagMs);
            if (lost)
                return;
            if (lagMs != 0)
                await UniTask.Delay(lagMs);

            action.Invoke();
        }

        private void GetNewLag(out bool lost, out int lagMs)
        {
            lost = _lagRandom.NextDouble() < _lagConfig.PacketLoss;
            lagMs = (int)NextGaussian(_lagConfig.DelayMs + _lagConfig.JitterMs, _lagConfig.JitterMs);
            lagMs = Math.Max(lagMs, _lagConfig.DelayMs);
            lagMs = Math.Min(lagMs, _lagConfig.DelayMs + MaxJitterMultiplier * _lagConfig.JitterMs);
        }

        private double NextGaussian(double mean = 0.0, double stdDev = 1.0)
        {
            var u1 = 1.0 - _lagRandom.NextDouble();
            var u2 = 1.0 - _lagRandom.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private IEnumerator Synchronization()
        {
            while (NotDisconnected())
            {
                _client.SendNtp();
                yield return _synchronizationDelay;
            }
        }

        private void OnNtpReceived(NtpData ntpData)
        {
            GetNewLag(out var lost, out var lagMs);
            if (lost)
                return;

            var rtt = TimeSpan.FromMilliseconds(2 * lagMs);
            rtt += ntpData.RoundTripDelay;
            var timeSynchronizationData = new TimeSynchronizationData
            {
                RoundTripDelay = rtt,
                LocalClockOffset = TimeSpan.Zero,
                UnreliableWaitingForFirstPing = false,
                UnreliableReceivedAnyPing = true,
                UnreliableReceivedPingLately = true,
                UnreliableLocalClockOffset = TimeSpan.Zero,
                UnreliableLastReceivedPingDateTime = DateTime.Now,
                UnreliableRoundTripDelay = rtt,
            };
            Synchronized?.Invoke(timeSynchronizationData);
        }

        private bool NotDisconnected() => !_playerDisconnected;
        public void Dispose() => _input?.Dispose();
    }
}
