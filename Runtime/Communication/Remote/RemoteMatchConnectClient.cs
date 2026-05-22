using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models.Public;
using Elympics.Core.Logger;
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
        public event Action<MatchInitialData> MatchJoinedWithMatchInitData;

        public event Action<Guid> MatchEndedWithMatchId;

        public event Action DisconnectedByServer;
        public event Action DisconnectedByClient;

        private readonly IGameServerClient _gameServerClient;

        private readonly string _userSecret;

        private UniTaskCompletionSource _connectingTcs;
        private bool _connected;

        private readonly ElympicsLoggerConfig _logger;

        public RemoteMatchConnectClient(
            IGameServerClient gameServerClient,
            string userSecret)
        {
            _logger = ElympicsLogger.Config.WithClassName(nameof(RemoteMatchConnectClient));
            _gameServerClient = gameServerClient;
            _userSecret = userSecret;
            _gameServerClient.Disconnected += OnDisconnectedByServer;
            _gameServerClient.MatchEnded += OnMatchEnded;
        }

        public UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct)
        {
            return !string.IsNullOrEmpty(_userSecret)
                ? ConnectAndJoinAsync(SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer, ct)
                : throw new ArgumentNullException(nameof(_userSecret));
        }

        public UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct) =>
            ConnectAndJoinAsync(SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator, ct);

        public void Disconnect()
        {
            var logger = _logger.WithMehodName();
            if (!_connected)
                return;
            _connected = false;
            logger.LogInfo("Disconnected by client.");

            DisconnectedByClient?.Invoke();
            _gameServerClient.Disconnect();
        }

        private void SetupCallbacksForJoiningCommon()
        {
            _gameServerClient.MatchJoined += OnMatchJoined;
            _gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
        }

        private void UnsetCallbacksForJoiningCommon()
        {
            _gameServerClient.MatchJoined -= OnMatchJoined;
            _gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
        }

        private void SetupCallbacksForJoiningAsPlayer()
        {
            _gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsPlayer;
            _gameServerClient.UserMatchAuthenticated += OnAuthenticatedMatchUserSecret;
            SetupCallbacksForJoiningCommon();
        }

        private void UnsetCallbacksForJoiningAsPlayer()
        {
            _gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsPlayer;
            _gameServerClient.UserMatchAuthenticated -= OnAuthenticatedMatchUserSecret;
            UnsetCallbacksForJoiningCommon();
        }

        private void SetupCallbacksForJoiningAsSpectator()
        {
            _gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsSpectator;
            _gameServerClient.AuthenticatedAsSpectator += OnAuthenticatedAsSpectator;
            SetupCallbacksForJoiningCommon();
        }

        private void UnsetCallbacksForJoiningAsSpectator()
        {
            _gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsSpectator;
            _gameServerClient.AuthenticatedAsSpectator -= OnAuthenticatedAsSpectator;
            UnsetCallbacksForJoiningCommon();
        }

        private async UniTask ConnectAndJoinAsync(Action setupCallbacks, Action unsetCallbacks, CancellationToken ct)
        {
            var logger = _logger.WithMehodName();
            if (_connectingTcs != null)
                throw new InvalidOperationException("Already connecting");
            if (_connected)
                throw new InvalidOperationException("Already connected");

            _connectingTcs = new UniTaskCompletionSource();
            setupCallbacks();

            logger.LogInfo("Connecting to game server...");

            try
            {
                await _gameServerClient.ConnectAsync(ct);
                await _connectingTcs.Task.AttachExternalCancellation(ct);
            }
            catch
            {
                ConnectingFailed?.Invoke();
                throw;
            }
            finally
            {
                TryDisconnectByServerIfNotConnected();
                unsetCallbacks();
                _connectingTcs = null;
            }
        }

        private void OnConnectedAndSynchronizedAsPlayer(TimeSynchronizationData timeSynchronizationData)
        {
            var logger = _logger.WithMehodName();
            logger.LogInfo("Connected and synchronized as player.");
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _gameServerClient.AuthenticateMatchUserSecretAsync(_userSecret);
        }

        private void OnConnectedAndSynchronizedAsSpectator(TimeSynchronizationData timeSynchronizationData)
        {
            var logger = _logger.WithMehodName();
            logger.LogInfo("Connected and synchronized as spectator.");
            ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
            _gameServerClient.AuthenticateAsSpectatorAsync();
        }

        private void OnAuthenticatedMatchUserSecret(UserMatchAuthenticatedMessage message)
        {
            var logger = _logger.WithMehodName();
            if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
            {
                logger.LogError($"Failed to authenticate user: {message.ErrorMessage}");
                AuthenticatedUserMatchFailedWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }
            logger.LogInfo("User authenticated.");
            AuthenticatedUserMatchWithUserId?.Invoke(message.UserId != null ? new Guid(message.UserId) : Guid.Empty);

            _gameServerClient.JoinMatchAsync();
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

            _gameServerClient.JoinMatchAsync();
        }

        private void OnMatchJoined(MatchJoinedMessage message)
        {
            var logger = _logger.WithMehodName();
            if (!string.IsNullOrEmpty(message.ErrorMessage))
            {
                logger.LogError($"Can't join match {message.MatchId}.{Environment.NewLine}Error: {message.ErrorMessage}");
                MatchJoinedWithError?.Invoke(message.ErrorMessage);
                _gameServerClient.Disconnect();
                return;
            }

            var matchInitData = message.Map();

            logger.LogInfo("Match joined.");
            MatchJoinedWithMatchInitData?.Invoke(matchInitData);
            _connected = true;
            _ = _connectingTcs?.TrySetResult();
        }

        private void OnMatchEnded(MatchEndedMessage message)
        {
            var logger = _logger.WithMehodName();
            logger.LogInfo("Match ended.");
            MatchEndedWithMatchId?.Invoke(new Guid(message.MatchId));
        }

        private void OnDisconnectedWhileConnectingAndJoining() =>
            _ = _connectingTcs?.TrySetException(new ElympicsException("Disconnected while connecting and joining"));

        private void OnDisconnectedByServer()
        {
            var logger = _logger.WithMehodName();
            logger.LogInfo("Disconnected by server.");
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
