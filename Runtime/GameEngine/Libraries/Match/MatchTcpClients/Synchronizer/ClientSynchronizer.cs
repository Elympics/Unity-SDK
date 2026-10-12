using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using MatchTcpLibrary;
using MatchTcpLibrary.Ntp;
using MatchTcpModels.Commands;
using MatchTcpModels.Messages;

namespace MatchTcpClients.Synchronizer
{
    internal class ClientSynchronizer : IClientSynchronizer
    {
        public event Action<PingClientCommand> ReliablePingGenerated;
        public event Action<AuthenticateUnreliableSessionTokenCommand> AuthenticateUnreliableGenerated;
        public event Action<PingClientCommand> UnreliablePingGenerated;
        public event Action<TimeSynchronizationData> Synchronized;
        public event Action TimedOut;

        private readonly ClientSynchronizerConfig _config;
        private readonly LoggerConfig _logger = ElympicsLogger.WithElympicsGameService()
            .WithClass(typeof(ClientSynchronizer))
            .WithMonitoringEnabled();
        private DateTime? _lastReceivedPingDataTime;
        private NtpData _lastReceivedUnreliableNtpData;
        private bool _waitingForFirstUnreliablePing = true;

        private Action<PingClientResponseMessage> _pingResponseCallback;

        public ClientSynchronizer(ClientSynchronizerConfig config)
        {
            _config = config;
        }

        public async UniTask StartContinuousSynchronizingAsync(string sessionToken, CancellationToken ct)
        {
            ClearUnreliablePingFlagAfterTimeout(ct).Forget();
            var logger = _logger.WithMethodName();
            logger.LogInfo("Starting client synchronization...");
            var stopwatch = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                stopwatch.Start();
                TimeSynchronizationData synchronizationData;
                try
                {
                    synchronizationData = await SynchronizeOnce(sessionToken, ct);
                }
                catch (TimeoutException)
                {
                    TimedOut?.Invoke();
                    continue;
                }
                stopwatch.Stop();
                if (ct.IsCancellationRequested)
                    break;

                Synchronized?.Invoke(synchronizationData);

                var timeToWait = _config.ContinuousSynchronizationMinimumInterval - stopwatch.Elapsed;
                stopwatch.Reset();

                if (timeToWait > TimeSpan.Zero)
                    _ = await UniTask.Delay(timeToWait, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();
            }
            logger.LogInfo("Ending client synchronization.");
        }

        private async UniTaskVoid ClearUnreliablePingFlagAfterTimeout(CancellationToken ct)
        {
            if (await UniTask.Delay(_config.UnreliablePingTimeoutInMilliseconds, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow())
                return;
            _waitingForFirstUnreliablePing = false;
        }

        public async UniTask<TimeSynchronizationData> SynchronizeOnce(string sessionToken, CancellationToken ct)
        {
            if (_pingResponseCallback != null)
                throw new InvalidOperationException("Cannot synchronize when there is other synchronization running");

            var pingCompletionSource = new UniTaskCompletionSource<PingClientResponseMessage>();
            _pingResponseCallback = response => pingCompletionSource.TrySetResult(response);

            try
            {
                SendSynchronizeRequest(sessionToken);

                var pingResult = await pingCompletionSource.Task.WithTimeout(_config.TimeoutTime, ct);
                return pingResult == null ? null : CreateSynchronizeResponse(pingResult);
            }
            finally
            {
                _pingResponseCallback = null;
            }
        }

        private void SendSynchronizeRequest(string sessionToken)
        {
            var ntpRequest = new NtpData { TransmitTimestamp = DateTime.UtcNow };
            var pingCommand = new PingClientCommand { NtpData = Convert.ToBase64String(ntpRequest.Data) };
            var authCommand = new AuthenticateUnreliableSessionTokenCommand { SessionToken = sessionToken };
            ReliablePingGenerated?.Invoke(pingCommand);
            UnreliablePingGenerated?.Invoke(pingCommand);
            AuthenticateUnreliableGenerated?.Invoke(authCommand);
        }

        private TimeSynchronizationData CreateSynchronizeResponse(PingClientResponseMessage pingResult) =>
            new(CreateNtpDataFromBytes(Convert.FromBase64String(pingResult.NtpData)))
            {
                UnreliableReceivedAnyPing = _lastReceivedPingDataTime != null,
                UnreliableLastReceivedPingDateTime = _lastReceivedPingDataTime,
                UnreliableReceivedPingLately = _lastReceivedPingDataTime.HasValue && _lastReceivedPingDataTime.Value.AddSeconds(_config.UnreliablePingTimeoutInMilliseconds.Seconds) > DateTime.Now,
                UnreliableWaitingForFirstPing = _waitingForFirstUnreliablePing,
                UnreliableLocalClockOffset = _lastReceivedUnreliableNtpData?.LocalClockOffset,
                UnreliableRoundTripDelay = _lastReceivedUnreliableNtpData?.RoundTripDelay,
            };

        public void ReliablePingReceived(PingClientResponseMessage message) => _pingResponseCallback?.Invoke(message);

        public void UnreliablePingReceived(PingClientResponseMessage message)
        {
            _waitingForFirstUnreliablePing = false;
            _lastReceivedPingDataTime = DateTime.Now;
            var ntpResponse = CreateNtpDataFromBytes(Convert.FromBase64String(message.NtpData));
            _lastReceivedUnreliableNtpData = ntpResponse;
        }

        private static NtpData CreateNtpDataFromBytes(byte[] data)
        {
            var ntpResponse = new NtpData();
            ntpResponse.SetFromBytes(data);
            ntpResponse.ReceptionTimestamp = DateTime.UtcNow;
            return ntpResponse;
        }
    }
}
