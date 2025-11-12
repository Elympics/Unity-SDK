using System;
using System.Collections;
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

        private Action _disconnectedCallback;
        private Action _matchJoinedCallback;

        private ElympicsLoggerContext _logger;
        public RemoteMatchConnectClient(
            IGameServerClient gameServerClient,
            ElympicsLoggerContext logger,
            string tcpUdpServerAddress,
            string webServerAddress,
            string userSecret,
            bool useWeb = false)
        {
            _logger = logger.WithContext(nameof(RemoteMatchConnectClient));
            _gameServerClient = gameServerClient;
            _tcpUdpServerAddress = tcpUdpServerAddress;
            _webServerAddress = webServerAddress;
            _userSecret = userSecret;
            _useWeb = useWeb;
            _gameServerClient.Disconnected += OnDisconnectedByServer;
            _gameServerClient.MatchEnded += OnMatchEnded;
        }

        public IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)
        {
            CheckAddress();
            return !string.IsNullOrEmpty(_userSecret) ? ConnectAndJoin(connectedCallback, SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer, ct) : throw new ArgumentNullException(nameof(_userSecret));
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

        public IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct)
        {
            CheckAddress();
            return ConnectAndJoin(connectedCallback, SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator, ct);
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

        private IEnumerator ConnectAndJoin(Action<bool> connectedCallback, Action setupCallbacks, Action unsetCallbacks, CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            if (_connecting)
            {
                connectedCallback.Invoke(false);
                yield break;
            }

            _connecting = true;

            if (_connected)
            {
                connectedCallback.Invoke(false);
                yield break;
            }

            setupCallbacks();

            void ConnectedCallback(bool connected)
            {
                // Connect callback handled by setupCallbacks()
                if (connected)
                    return;

                ConnectingFailed?.Invoke();
                connectedCallback?.Invoke(false);
            }

            void DisconnectedCallback()
            {
                FinishConnecting(unsetCallbacks);
                connectedCallback.Invoke(false);
            }

            void MatchJoinedCallback()
            {
                _connected = true;
                FinishConnecting(unsetCallbacks);
                connectedCallback.Invoke(true);
            }

            _disconnectedCallback = DisconnectedCallback;
            _matchJoinedCallback = MatchJoinedCallback;

            logger.Log(_useWeb ? $"Connecting to game server by WebSocket/WebRTC" : $"Connecting to game server by TCP/UDP");

            yield return UniTask.ToCoroutine(() => _gameServerClient.ConnectAsync(ct).AsUniTask().ContinueWith(ConnectedCallback));
        }

        private void FinishConnecting(Action unsetCallbacks)
        {
            _connecting = false;
            TryDisconnectByServerIfNotConnected();
            unsetCallbacks();
            _disconnectedCallback = null;
            _matchJoinedCallback = null;
        }

        private void OnConnectedAndSynchronizedAsPlayer(TimeSynchronizationData timeSynchronizationData)
        {
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _ = _gameServerClient.AuthenticateMatchUserSecretAsync(_userSecret);
        }

        private void OnConnectedAndSynchronizedAsSpectator(TimeSynchronizationData timeSynchronizationData)
        {
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _ = _gameServerClient.AuthenticateAsSpectatorAsync();
        }

        private void OnAuthenticatedMatchUserSecret(UserMatchAuthenticatedMessage message)
        {
            if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
            {
                AuthenticatedUserMatchFailedWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }

            AuthenticatedUserMatchWithUserId?.Invoke(message.UserId != null ? new Guid(message.UserId) : Guid.Empty);

            _ = _gameServerClient.JoinMatchAsync();
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

            _ = _gameServerClient.JoinMatchAsync();
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
            _matchJoinedCallback?.Invoke();
        }

        private void OnMatchEnded(MatchEndedMessage message)
        {
            var logger = _logger.WithMethodName();
            logger.Log($"Match Ended.");
            MatchEndedWithMatchId?.Invoke(new Guid(message.MatchId));
        }

        private void OnDisconnectedWhileConnectingAndJoining()
        {
            _disconnectedCallback?.Invoke();
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
