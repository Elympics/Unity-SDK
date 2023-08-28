using System;
using System.Collections;
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

        private readonly RingBuffer<ElympicsInput> _inputRingBuffer;

        private readonly WaitForSeconds _synchronizationDelay;
        private readonly HalfRemoteLagConfig _lagConfig;
        private readonly Random _lagRandom;

        private string _userId;
        private HalfRemoteMatchClient _client;
        private bool _playerDisconnected;

        public HalfRemoteMatchClientAdapter(ElympicsGameConfig config)
        {
            _inputRingBuffer = new RingBuffer<ElympicsInput>(config.InputsToSendBufferSize);
            _lagConfig = config.HalfRemoteLagConfig;
            _lagRandom = new Random(config.HalfRemoteLagConfig.RandomSeed);
        }

        internal IEnumerator ConnectToServer(Action<bool> connectedCallback, string userId, HalfRemoteMatchClient client)
        {
            _userId = userId;

            _client = client;
            _client.ReliableReceivingError += Debug.LogError;
            _client.ReliableReceivingEnded += () =>
            {
                _playerDisconnected = true;
                Debug.Log("Reliable receiving ended");
            };
            _client.UnreliableReceivingError += Debug.LogError;
            _client.UnreliableReceivingEnded += () => Debug.Log("Unreliable receiving ended");
            _client.WebRtcUpgraded += () => Debug.Log("WebRtc upgraded");
            _client.NtpReceived += OnNtpReceived;
            _client.InGameDataForPlayerOnReliableChannelGenerated += OnReliableInGameDataReceived;
            _client.InGameDataForPlayerOnUnreliableChannelGenerated += OnUnreliableInGameDataReceived;

            Debug.Log("Connected with half remote client");
            connectedCallback?.Invoke(true);

            return Synchronization();
        }

        public void PlayerConnected() => _client.PlayerConnected();
        public void PlayerDisconnected() => _client?.PlayerDisconnected();

        public async Task SendInput(ElympicsInput input)
        {
            _inputRingBuffer.PushBack(input);
            var data = new ElympicsInputList { Values = _inputRingBuffer.ToList() };
            await SendRawDataToServer(MessagePackSerializer.Serialize<IToServer>(data), false);
        }

        public async Task SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            await SendRawDataToServer(MessagePackSerializer.Serialize<IToServer>(rpcMessageList), true);

        public async Task SendRawDataToServer(byte[] rawData, bool reliable)
        {
            Action<byte[]> sendDataAsync = reliable
                ? _client.SendInputReliable
                : _client.SendInputUnreliable;

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
            if (deserializedData is ElympicsSnapshot snapshot)
                SnapshotReceived?.Invoke(snapshot);
            else if (deserializedData is ElympicsRpcMessageList rpcMessageList)
                RpcMessageListReceived?.Invoke(rpcMessageList);
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
    }
}
