using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models.Public;
using Elympics.ElympicsSystems.Internal;
using Elympics.Mappers;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace Elympics
{
    internal class RemoteMatchConnectClient : IMatchConnectClient
    {
        public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
        public event Action ConnectingFailed;

        public event Action<Guid> AuthenticatedUserMatchWithUserId;
        public event Action<string> AuthenticatedUserMatchFailedWithError;

        public event Action AuthenticatedAsSpectator;
        public event Action<string> AuthenticatedAsSpectatorWithError;

        public event Action<string> MatchJoinedWithError;
        public event Action<Guid> MatchJoinedWithMatchId;
        public event Action<MatchInitialData> MatchJoinedWithMatchInitData;

        public event Action<Guid> MatchEndedWithMatchId;

        public event Action DisconnectedByServer;
        public event Action DisconnectedByClient;

        private readonly IGameServerClient _gameServerClient;

        private readonly string _tcpUdpServerAddress;
        private readonly string _webServerAddress;
        private readonly string _userSecret;
        private readonly bool _useWeb;

        private bool _connecting;
        private bool _connected;

        private UniTaskCompletionSource _connectAndJoinTcs;

        private readonly ElympicsLoggerContext _logger;

        public RemoteMatchConnectClient(
            IGameServerClient gameServerClient,
            string tcpUdpServerAddress,
            string webServerAddress,
            string userSecret,
            bool useWeb = false)
        {
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(RemoteMatchConnectClient));
            _gameServerClient = gameServerClient;
            _tcpUdpServerAddress = tcpUdpServerAddress;
            _webServerAddress = webServerAddress;
            _userSecret = userSecret;
            _useWeb = useWeb;
            _gameServerClient.Disconnected += OnDisconnectedByServer;
            _gameServerClient.MatchEnded += OnMatchEnded;
        }

        public async UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct)
        {
            CheckAddress();
            if (string.IsNullOrEmpty(_userSecret))
                throw new ArgumentNullException(nameof(_userSecret));
            await ConnectAndJoinAsync(SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer, ct);
        }

        private void CheckAddress()
        {
            if (_useWeb)
            {
                if (string.IsNullOrEmpty(_webServerAddress))
                    throw new ArgumentNullException(nameof(_webServerAddress));
            }
            else
            {
                if (string.IsNullOrEmpty(_tcpUdpServerAddress))
                    throw new ArgumentNullException(nameof(_tcpUdpServerAddress));
            }
        }

        public async UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct)
        {
            CheckAddress();
            await ConnectAndJoinAsync(SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator, ct);
        }

        public void Disconnect()
        {
            var logger = _logger.WithMethodName();
            if (!_connected)
                return;
            _connected = false;
            logger.Log("Disconnected by client.");

            DisconnectedByClient?.Invoke();
            _gameServerClient.Disconnect();
        }

        private void SetupCallbacksForJoiningAsPlayer()
        {
            _gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsPlayer;
            _gameServerClient.UserMatchAuthenticated += OnAuthenticatedMatchUserSecret;
            _gameServerClient.MatchJoined += OnMatchJoined;
            _gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
        }

        private void UnsetCallbacksForJoiningAsPlayer()
        {
            _gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsPlayer;
            _gameServerClient.UserMatchAuthenticated -= OnAuthenticatedMatchUserSecret;
            _gameServerClient.MatchJoined -= OnMatchJoined;
            _gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
        }

        private void SetupCallbacksForJoiningAsSpectator()
        {
            _gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsSpectator;
            _gameServerClient.AuthenticatedAsSpectator += OnAuthenticatedAsSpectator;
            _gameServerClient.MatchJoined += OnMatchJoined;
            _gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
        }

        private void UnsetCallbacksForJoiningAsSpectator()
        {
            _gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsSpectator;
            _gameServerClient.AuthenticatedAsSpectator -= OnAuthenticatedAsSpectator;
            _gameServerClient.MatchJoined -= OnMatchJoined;
            _gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
        }

        private async UniTask ConnectAndJoinAsync(Action setupCallbacks, Action unsetCallbacks, CancellationToken ct)
        {
            var logger = _logger.WithMethodName();
            if (_connecting)
                throw new InvalidOperationException("Already connecting");
            if (_connected)
                throw new InvalidOperationException("Already connected");

            _connecting = true;
            _connectAndJoinTcs = new UniTaskCompletionSource();
            setupCallbacks();

            logger.Log(_useWeb ? "Connecting to game server by WebSocket/WebRTC" : "Connecting to game server by TCP/UDP");

            try
            {
                var connected = await _gameServerClient.ConnectAsync(ct);
                if (!connected)
                {
                    ConnectingFailed?.Invoke();
                    throw new ElympicsException("Failed to connect to game server");
                }

                await _connectAndJoinTcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                FinishConnecting(unsetCallbacks);
            }
        }

        private void FinishConnecting(Action unsetCallbacks)
        {
            _connecting = false;
            TryDisconnectByServerIfNotConnected();
            unsetCallbacks();
            _connectAndJoinTcs = null;
        }

        private void OnConnectedAndSynchronizedAsPlayer(TimeSynchronizationData timeSynchronizationData)
        {
            var logger = _logger.WithMethodName();
            logger.Log("Connected And Synchronized as player.");
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _gameServerClient.AuthenticateMatchUserSecretAsync(_userSecret).Forget();
        }

        private void OnConnectedAndSynchronizedAsSpectator(TimeSynchronizationData timeSynchronizationData)
        {
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _gameServerClient.AuthenticateAsSpectatorAsync().Forget();
        }

        private void OnAuthenticatedMatchUserSecret(UserMatchAuthenticatedMessage message)
        {
            var logger = _logger.WithMethodName();
            if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
            {
                logger.Error($"Failed to authenticate user. {message.ErrorMessage}");
                AuthenticatedUserMatchFailedWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }
            logger.Log("User Authenticated.");
            AuthenticatedUserMatchWithUserId?.Invoke(message.UserId != null ? new Guid(message.UserId) : Guid.Empty);

            _gameServerClient.JoinMatchAsync().Forget();
        }

        private void OnAuthenticatedAsSpectator(AuthenticatedAsSpectatorMessage message)
        {
            if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
            {
                AuthenticatedAsSpectatorWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }

            AuthenticatedAsSpectator?.Invoke();

            _gameServerClient.JoinMatchAsync().Forget();
        }

        private void OnMatchJoined(MatchJoinedMessage message)
        {
            var logger = _logger.WithMethodName();
            if (!string.IsNullOrEmpty(message.ErrorMessage))
            {
                logger.Error($"Can't join match {message.MatchId}.{Environment.NewLine}Error: {message.ErrorMessage}");
                MatchJoinedWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }

            var matchInitData = message.Map();

            logger.Log($"Match joined.");
            MatchJoinedWithMatchInitData?.Invoke(matchInitData);
            _connected = true;
            _connectAndJoinTcs?.TrySetResult();
        }

        private void OnMatchEnded(MatchEndedMessage message)
        {
            var logger = _logger.WithMethodName();
            logger.Log($"Match Ended.");
            MatchEndedWithMatchId?.Invoke(new Guid(message.MatchId));
        }

        private void OnDisconnectedWhileConnectingAndJoining()
        {
            _connectAndJoinTcs?.TrySetException(new ElympicsException("Disconnected while connecting and joining"));
        }

        private void OnDisconnectedByServer()
        {
            if (_connecting)
                return;
            var logger = _logger.WithMethodName();
            logger.Log("Disconnected by server.");
            TryDisconnectByServerIfNotConnected();
        }

        private void TryDisconnectByServerIfNotConnected()
        {
            if (!_connected)
                return;
            if (_gameServerClient.IsConnected)
                return;
            DisconnectedByServer?.Invoke();
            _connected = false;
        }

        public void Dispose() => Disconnect();
    }
}
