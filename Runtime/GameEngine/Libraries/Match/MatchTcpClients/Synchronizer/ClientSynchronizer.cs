using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpLibrary.Ntp;
using MatchTcpModels.Commands;
using MatchTcpModels.Messages;

namespace MatchTcpClients.Synchronizer
{
	internal class ClientSynchronizer : IClientSynchronizer
	{
		public event Action<PingClientCommand>                         ReliablePingGenerated;
		public event Action<AuthenticateUnreliableSessionTokenCommand> AuthenticateUnreliableGenerated;
		public event Action<PingClientCommand>                         UnreliablePingGenerated;
		public event Action<TimeSynchronizationData>                   Synchronized;
		public event Action                                            TimedOut;

		private readonly ClientSynchronizerConfig _config;
		private          string                   _sessionToken;
		private          DateTime?                _lastReceivedPingDataTime;
		private          NtpData                  _lastReceivedUnreliableNtpData;
		private          bool                     _waitingForFirstUnreliablePing = true;

		private Action<PingClientResponseMessage> _pingResponseCallback;

		public ClientSynchronizer(ClientSynchronizerConfig config)
		{
			_config = config;
		}

		public async Task StartContinuousSynchronizingAsync(CancellationToken ct)
		{
			_ = Task.Run(async () =>
			{
				await Task.Delay(_config.UnreliablePingTimeoutInMilliseconds, ct);
				_waitingForFirstUnreliablePing = false;
			}, ct);

			var stopwatch = new Stopwatch();
			while (!ct.IsCancellationRequested)
			{
				stopwatch.Start();
				var synchronizationData = await SynchronizeOnce(ct);
				stopwatch.Stop();

				if (synchronizationData == null)
				{
					if (!ct.IsCancellationRequested)
						TimedOut?.Invoke();
				}
				else
				{
					Synchronized?.Invoke(synchronizationData);

					var timeToWait = _config.ContinuousSynchronizationMinimumInterval - stopwatch.Elapsed;
					stopwatch.Reset();

					if (timeToWait > TimeSpan.Zero)
						await Task.Delay(timeToWait, ct).CatchOperationCanceledException();
				}
			}
		}

		public async Task<TimeSynchronizationData> SynchronizeOnce(CancellationToken ct)
		{
			if (_pingResponseCallback != null)
				throw new ArgumentException("Cannot synchronize when there is other synchronization running");

			var pingCompletionSource = new TaskCompletionSource<PingClientResponseMessage>();
			_pingResponseCallback = response => pingCompletionSource?.TrySetResult(response);

			SendSynchronizeRequest();

			var pingCompletionTask = pingCompletionSource.Task;
			var timeoutTask = Task.Delay(_config.TimeoutTime, ct).CatchOperationCanceledException();

			var firstFinishedTask = await Task.WhenAny(pingCompletionTask, timeoutTask);
			_pingResponseCallback = null;
			pingCompletionSource = null;

			if (firstFinishedTask == timeoutTask)
				return null;

			var pingResult = await pingCompletionTask;
			return pingResult == null ? null : CreateSynchronizeResponse(pingResult);
		}

		private void SendSynchronizeRequest()
		{
			var ntpRequest = new NtpData {TransmitTimestamp = DateTime.UtcNow};
			var pingCommand = new PingClientCommand {NtpData = Convert.ToBase64String(ntpRequest.Data)};
			var authCommand = new AuthenticateUnreliableSessionTokenCommand {SessionToken = _sessionToken};
			ReliablePingGenerated?.Invoke(pingCommand);
			UnreliablePingGenerated?.Invoke(pingCommand);
			AuthenticateUnreliableGenerated?.Invoke(authCommand);
		}

		private TimeSynchronizationData CreateSynchronizeResponse(PingClientResponseMessage pingResult)
		{
			var ntpResponse = CreateNtpDataFromBytes(Convert.FromBase64String(pingResult.NtpData));

			var timeSynchronizationData = new TimeSynchronizationData
			{
				LocalClockOffset = ntpResponse.LocalClockOffset,
				RoundTripDelay = ntpResponse.RoundTripDelay,
				UnreliableReceivedAnyPing = _lastReceivedPingDataTime != null,
				UnreliableLastReceivedPingDateTime = _lastReceivedPingDataTime,
				UnreliableReceivedPingLately = _lastReceivedPingDataTime.HasValue && _lastReceivedPingDataTime.Value.AddSeconds(_config.UnreliablePingTimeoutInMilliseconds.Seconds) > DateTime.Now,
				UnreliableWaitingForFirstPing = _waitingForFirstUnreliablePing,
				UnreliableLocalClockOffset = _lastReceivedUnreliableNtpData?.LocalClockOffset,
				UnreliableRoundTripDelay = _lastReceivedUnreliableNtpData?.RoundTripDelay,
			};
			return timeSynchronizationData;
		}

		public void ReliablePingReceived(PingClientResponseMessage message)
		{
			_pingResponseCallback?.Invoke(message);
		}

		public void UnreliablePingReceived(PingClientResponseMessage message)
		{
			_waitingForFirstUnreliablePing = false;
			_lastReceivedPingDataTime = DateTime.Now;
			var ntpResponse = CreateNtpDataFromBytes(Convert.FromBase64String(message.NtpData));
			_lastReceivedUnreliableNtpData = ntpResponse;
		}

		public void SetUnreliableSessionToken(string sessionToken) => _sessionToken = sessionToken;

		private NtpData CreateNtpDataFromBytes(byte[] data)
		{
			var ntpResponse = new NtpData();
			ntpResponse.SetFromBytes(data);
			ntpResponse.ReceptionTimestamp = DateTime.UtcNow;
			return ntpResponse;
		}
	}
}
