#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.ElympicsSystems.Internal;
using MatchTcpClients.Synchronizer;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpModels.Commands;
using MatchTcpModels.Messages;

namespace MatchTcpClients
{
    internal abstract class GameServerClient : IGameServerClient
    {
        protected readonly GameServerClientConfig Config;

        public bool IsConnected => ReliableClient?.IsConnected ?? false;

        protected CancellationTokenSource? ClientDisconnectedCts;
        protected IReliableNetworkClient? ReliableClient;
        protected IUnreliableNetworkClient? UnreliableClient;

        private UniTaskCompletionSource<ConnectedMessage>? _sessionConnectedTcs;

        private readonly IGameServerSerializer _serializer;
        private readonly IClientSynchronizer _clientSynchronizer;
        private readonly ElympicsLoggerContext _logger;

        public event Action? Connected;
        public event Action<TimeSynchronizationData>? ConnectedAndSynchronized;
        public event Action<TimeSynchronizationData>? Synchronized;
        public event Action? Disconnected;
        public event Action<UserMatchAuthenticatedMessage>? UserMatchAuthenticated;
        public event Action<AuthenticatedAsSpectatorMessage>? AuthenticatedAsSpectator;
        public event Action<MatchJoinedMessage>? MatchJoined;
        public event Action<MatchEndedMessage>? MatchEnded;
        public event Action<InGameDataMessage>? InGameDataReliableReceived;
        public event Action<InGameDataMessage>? InGameDataUnreliableReceived;

        protected GameServerClient(IGameServerSerializer serializer, GameServerClientConfig config)
        {
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(GameServerClient));
            Config = config;
            _serializer = serializer;
            _clientSynchronizer = new ClientSynchronizer(config.ClientSynchronizerConfig);
            _clientSynchronizer.ReliablePingGenerated += command => SendReliableCommand(command).Forget();
            _clientSynchronizer.UnreliablePingGenerated += command => SendUnreliableCommand(command).Forget();
            _clientSynchronizer.AuthenticateUnreliableGenerated += command => SendUnreliableCommand(command).Forget();
            _clientSynchronizer.Synchronized += data => Synchronized?.Invoke(data);
            _clientSynchronizer.TimedOut += OnTimeout;
        }

        protected void Initialize()
        {
            ReliableClient?.Dispose();
            ReliableClient = null;
            UnreliableClient?.Dispose();
            UnreliableClient = null;
            (ReliableClient, UnreliableClient) = CreateNetworkClients();

            ClientDisconnectedCts?.Cancel();
            ClientDisconnectedCts?.Dispose();
            ClientDisconnectedCts = new CancellationTokenSource();
            _ = ClientDisconnectedCts.Token.Register(() => Disconnected?.Invoke());

            InitializeNetworkClients(ReliableClient, UnreliableClient);
            ReliableClient.CreateAndBind();
            UnreliableClient.CreateAndBind();
        }

        public async UniTask ConnectAsync(CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            Disconnect();
            ClientDisconnectedCts = new CancellationTokenSource();

            try
            {
                await ConnectInternalAsync(ct);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.Exception(new ElympicsException("Failed to connect", e));
                throw;
            }
            InvokeSafely(Connected, logger);

            ConnectedMessage connectedMessage;
            try
            {
                connectedMessage = await ConnectSessionAsync(ct);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.Exception(new ElympicsException("Failed to connect", e));
                throw;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, ClientDisconnectedCts.Token);
            var sessionToken = connectedMessage.SessionToken ?? "";
            TimeSynchronizationData synchronizationData;
            try
            {
                Func<UniTask<TimeSynchronizationData>> func = () => _clientSynchronizer.SynchronizeOnce(sessionToken, ct);
                synchronizationData = await func.WithRetry(Config.InitialSynchronizeMaxRetries,
                    onRetry: i => logger.Log($"Could not perform initial synchronization, retrying... #{i}"), ct: linkedCts.Token);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.Exception(new ElympicsException("Failed to perform initial synchronization", e));
                throw;
            }

            InvokeSafely(ConnectedAndSynchronized, synchronizationData, logger);
            _clientSynchronizer.StartContinuousSynchronizingAsync(sessionToken, ClientDisconnectedCts.Token).Forget();
        }

        protected abstract UniTask ConnectInternalAsync(CancellationToken ct = default);

        protected async UniTask<ConnectedMessage> ConnectSessionAsync(CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            _sessionConnectedTcs = new UniTaskCompletionSource<ConnectedMessage>();

            try
            {
                await InitializeSessionAsync(ct);
            }
            catch
            {
                _sessionConnectedTcs = null;
                throw;
            }

            logger.Log("Connecting to reliable channel...");
            return await _sessionConnectedTcs.Task.WithTimeout(Config.SessionConnectTimeout, ct);
        }

        protected abstract UniTask InitializeSessionAsync(CancellationToken ct = default);

        private static void InvokeSafely(Action? action, ElympicsLoggerContext logger)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private static void InvokeSafely<T>(Action<T>? action, T arg, ElympicsLoggerContext logger)
        {
            try
            {
                action?.Invoke(arg);
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        protected abstract (IReliableNetworkClient, IUnreliableNetworkClient) CreateNetworkClients();

        protected virtual void InitializeNetworkClients(IReliableNetworkClient reliable, IUnreliableNetworkClient unreliable)
        {
            if (ClientDisconnectedCts == null)
                throw new InvalidOperationException();
            reliable.DataReceived += OnReliableMessageDataReceived;
            reliable.Disconnected += Disconnect;
            _ = ClientDisconnectedCts.Token.Register(reliable.Disconnect);
            unreliable.DataReceived += OnUnreliableMessageDataReceived;
            _ = ClientDisconnectedCts.Token.Register(unreliable.Disconnect);
        }

        private void OnTimeout()
        {
            var log = _logger.WithMethodName();
            log.Error("Synchronize timed out, disconnecting...");
            Disconnect();
        }

        public void Disconnect()
        {
            if (ClientDisconnectedCts == null)
                return;
            var logger = _logger.WithMethodName();
            logger.Log("Aborting connection.");
            ClientDisconnectedCts.Cancel();
            ClientDisconnectedCts.Dispose();
            ClientDisconnectedCts = null;
            _clientSynchronizer.TimedOut -= OnTimeout;
        }

        private async UniTask SendReliableCommand(Command command) => await ReliableClient.SendAsync(_serializer.Serialize(command));

        private void OnReliableMessageDataReceived(byte[] data)
        {
            try
            {
                var message = _serializer.Deserialize<Message>(data);
                OnReliableMessageDataReceived(data, message.Type);
            }
            catch (Exception e)
            {
                var log = _logger.WithMethodName();
                log.Exception(new ElympicsException($"Error in {GetType().Name} receiving a message using reliable channel", e));
            }
        }

        private void OnReliableMessageDataReceived(byte[] data, MessageType type)
        {
            switch (type)
            {
                case MessageType.Connected:
                    _ = InvokeReceivedMessageEvent<ConnectedMessage>(data, OnSessionConnected);
                    break;
                case MessageType.PingServer:
                    RespondForPing();
                    break;
                case MessageType.InGameData:
                    _ = InvokeReceivedMessageEvent(data, InGameDataReliableReceived);
                    break;
                case MessageType.UserMatchAuthenticatedMessage:
                    _ = InvokeReceivedMessageEvent(data, UserMatchAuthenticated);
                    break;
                case MessageType.AuthenticateAsSpectator:
                    _ = InvokeReceivedMessageEvent(data, AuthenticatedAsSpectator);
                    break;
                case MessageType.MatchJoined:
                    _ = InvokeReceivedMessageEvent(data, MatchJoined);
                    break;
                case MessageType.MatchEnded:
                    _ = InvokeReceivedMessageEvent(data, MatchEnded);
                    break;
                case MessageType.PingClientResponse:
                    var pingClientResponseMessage = _serializer.Deserialize<PingClientResponseMessage>(data);
                    _clientSynchronizer.ReliablePingReceived(pingClientResponseMessage);
                    break;
                case MessageType.UnknownCommandMessage:
                    ElympicsLogger.LogError(_serializer.Deserialize<UnknownCommandMessage>(data).ErrorMessage);
                    break;
                case MessageType.None:
                default:
                    break;
            }

            void OnSessionConnected(ConnectedMessage message)
            {
                _ = _sessionConnectedTcs?.TrySetResult(message);
            }
        }

        private void RespondForPing() => _ = SendReliableCommand(new PingServerResponseCommand());

        private T InvokeReceivedMessageEvent<T>(byte[] data, Action<T>? action)
        {
            var message = _serializer.Deserialize<T>(data);
            if (message != null)
                action?.Invoke(message);
            return message;
        }

        public async UniTask AuthenticateMatchUserSecretAsync(string userSecret) =>
            await SendReliableCommand(new AuthenticateMatchUserSecretCommand { UserSecret = userSecret });

        public async UniTask AuthenticateAsSpectatorAsync() =>
            await SendReliableCommand(new AuthenticateAsSpectatorCommand());

        public async UniTask JoinMatchAsync() =>
            await SendReliableCommand(new JoinMatchCommand());

        public async UniTask SendInGameDataReliableAsync(byte[] data) =>
            await SendReliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });

        public async UniTask SendInGameDataUnreliableAsync(byte[] data) =>
            await SendUnreliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });

        private async UniTask SendUnreliableCommand(object command) =>
            await UnreliableClient.SendAsync(_serializer.Serialize(command));

        private void OnUnreliableMessageDataReceived(byte[] data)
        {
            try
            {
                var message = _serializer.Deserialize<Message>(data);
                OnUnreliableMessageDataReceived(data, message.Type);
            }
            catch (Exception e)
            {
                var log = _logger.WithMethodName();
                log.Exception(new ElympicsException($"Error in {GetType().Name} receiving a message using unreliable channel", e));
            }
        }

        private void OnUnreliableMessageDataReceived(byte[] data, MessageType type)
        {
            switch (type)
            {
                case MessageType.InGameData:
                    _ = InvokeReceivedMessageEvent(data, InGameDataUnreliableReceived);
                    break;
                case MessageType.PingClientResponse:
                    _clientSynchronizer.UnreliablePingReceived(_serializer.Deserialize<PingClientResponseMessage>(data));
                    break;
                case MessageType.None:
                    break;
                case MessageType.UnknownCommandMessage:
                    ElympicsLogger.LogError(_serializer.Deserialize<UnknownCommandMessage>(data).ErrorMessage);
                    break;
                case MessageType.Connected:
                case MessageType.PingServer:
                case MessageType.MatchJoined:
                case MessageType.UserMatchAuthenticatedMessage:
                case MessageType.MatchEnded:
                case MessageType.AuthenticateAsSpectator:
                default:
                    break;
            }
        }
    }
}
