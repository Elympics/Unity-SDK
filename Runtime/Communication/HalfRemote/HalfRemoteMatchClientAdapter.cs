using System;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using UnityConnectors.HalfRemote;
using UnityConnectors.HalfRemote.Ntp;
using UnityEngine;
using Random = System.Random;

namespace Elympics
{
	public class HalfRemoteMatchClientAdapter : IMatchClient
	{
		private static readonly WaitForSeconds SynchronizationDelay = new WaitForSeconds(1);
		private const           int            MaxJitterMultiplier  = 3;

		public event Action<TimeSynchronizationData> Synchronized;
		public event Action<ElympicsSnapshot>        SnapshotReceived;
		public event Action<byte[]>                  RawSnapshotReceived;

		private readonly RingBuffer<byte[]> _inputRingBuffer;

		private readonly HalfRemoteLagConfig _lagConfig;
		private readonly Random              _lagRandom;

		private string                _userId;
		private HalfRemoteMatchClient _client;
		private bool                  _playerDisconnected;

		public HalfRemoteMatchClientAdapter(ElympicsGameConfig config)
		{
			_inputRingBuffer = new RingBuffer<byte[]>(config.InputsToSendBufferSize);
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
			_client.UnreliableReceivingEnded += () => { Debug.Log("Unreliable receiving ended"); };
			_client.WebRtcUpgraded += () => Debug.Log("WebRtc upgraded");
			_client.NtpReceived += OnNtpReceived;
			_client.InGameDataForPlayerOnReliableChannelGenerated += OnInGameDataForPlayerOnReliableChannelGenerated;
			_client.InGameDataForPlayerOnUnreliableChannelGenerated += OnInGameDataForPlayerOnUnreliableChannelGenerated;

			Debug.Log("Connected with half remote client");
			connectedCallback?.Invoke(true);

			return Synchronization();
		}

		public void PlayerConnected()    => _client.PlayerConnected();
		public void PlayerDisconnected() => _client?.PlayerDisconnected();

		public Task SendInputReliable(ElympicsInput input)
		{
			SendRawInputReliable(input.Serialize());
			return Task.CompletedTask;
		}

		public Task SendInputUnreliable(ElympicsInput input)
		{
			SendRawInputUnreliable(input.Serialize());
			return Task.CompletedTask;
		}

		public void OnInGameDataForPlayerOnReliableChannelGenerated(byte[] data, string userId)
		{
			if (userId != _userId)
				return;
			_ = RunWithLag(() =>
			{
				SnapshotReceived?.Invoke(ElympicsSnapshotSerializer.Deserialize(data));
				RawSnapshotReceived?.Invoke(data);
			});
		}

		public void OnInGameDataForPlayerOnUnreliableChannelGenerated(byte[] data, string userId)
		{
			if (userId != _userId)
				return;
			_ = RunWithLag(() =>
			{
				SnapshotReceived?.Invoke(ElympicsSnapshotSerializer.Deserialize(data));
				RawSnapshotReceived?.Invoke(data);
			});
		}

		public void SendRawInputReliable(byte[] data) => _ = RunWithLag(() => _client.SendInputReliable(data));

		public void SendRawInputUnreliable(byte[] data)
		{
			_inputRingBuffer.PushBack(data);
			var serializedInputs = ElympicsInputSerializer.MergeInputsToPackage(_inputRingBuffer.ToArray());
			_ = RunWithLag(() => _client.SendInputUnreliable(serializedInputs));
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
			lagMs = (int) NextGaussian(_lagConfig.DelayMs + _lagConfig.JitterMs, _lagConfig.JitterMs);
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
				yield return SynchronizationDelay;
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
