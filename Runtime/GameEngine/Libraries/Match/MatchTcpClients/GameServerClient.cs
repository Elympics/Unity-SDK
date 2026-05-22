#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Core.Logger;
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

        public bool IsConnected => NetworkClient?.IsConnected ?? false;

        private CancellationTokenSource? _clientDisconnectedCts;
        protected INetworkClient? NetworkClient;

        private UniTaskCompletionSource<ConnectedMessage>? _sessionConnectedTcs;

        private readonly IGameServerSerializer _serializer;
        private IClientSynchronizer _clientSynchronizer = null!;
        private readonly ApplicationState _logger;

        public event Action? Connected;
        public event Action<TimeSynchronizationData>? ConnectedAndSynchronized;
        public event Action<TimeSynchronizationData>? Synchronized;
        public event Action? Disconnected;
        public event Action<UserMatchAuthenticatedMessage>? UserMatchAuthenticated;
        public event Action<AuthenticatedAsSpectatorMessage>? AuthenticatedAsSpectator;
        public event Action<MatchJoinedMessage>? MatchJoined;
        public event Action<MatchEndedMessage>? MatchEnded;
        public event Action<string, InGameDataMessage>? InGameDataReceived;

        protected GameServerClient(IGameServerSerializer serializer, GameServerClientConfig config)
        {
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(GameServerClient));
            Config = config;
            _serializer = serializer;
        }

        private void Initialize()
        {
            NetworkClient?.Dispose();
            NetworkClient = null;
            NetworkClient = CreateNetworkClient();

            _clientDisconnectedCts?.Cancel();
            _clientDisconnectedCts?.Dispose();
            _clientDisconnectedCts = new CancellationTokenSource();
            _ = _clientDisconnectedCts.Token.Register(() => Disconnected?.Invoke());

            InitializeNetworkClient(NetworkClient);
            NetworkClient.CreateAndBind();

            _clientSynchronizer = new ClientSynchronizer(Config.ClientSynchronizerConfig);
            _clientSynchronizer.ReliablePingGenerated += SendReliableCommand;
            _clientSynchronizer.UnreliablePingGenerated += SendUnreliableCommand;
            _clientSynchronizer.AuthenticateUnreliableGenerated += SendUnreliableCommand;
            _clientSynchronizer.Synchronized += data => Synchronized?.Invoke(data);
            _clientSynchronizer.TimedOut += OnTimeout;
        }

        public async UniTask ConnectAsync(CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            Disconnect();
            Initialize();

            // Must exist before the transport connects: the server can send its Connected message
            // as soon as the socket/channel is up, which can race ahead of a TCS created afterward.
            _sessionConnectedTcs = new UniTaskCompletionSource<ConnectedMessage>();

            try
            {
                await ConnectInternalAsync(ct);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.LogException(new ElympicsException("Failed to connect", e));
                throw;
            }
            InvokeSafely(Connected, logger);

            ConnectedMessage connectedMessage;
            try
            {
                logger.LogInfo("Connecting to reliable channel...");
                connectedMessage = await _sessionConnectedTcs.Task.WithTimeout(Config.SessionConnectTimeout, ct);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.LogException(new ElympicsException("Failed to connect", e));
                throw;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _clientDisconnectedCts!.Token);
            var sessionToken = connectedMessage.SessionToken ?? "";
            TimeSynchronizationData synchronizationData;
            try
            {
                Func<UniTask<TimeSynchronizationData>> func = () => _clientSynchronizer.SynchronizeOnce(sessionToken, ct);
                synchronizationData = await func.WithRetry(Config.InitialSynchronizeMaxRetries,
                    onRetry: i => logger.LogInfo($"Could not perform initial synchronization, retrying... #{i}"), ct: linkedCts.Token);
            }
            catch (Exception e)
            {
                Disconnect();
                logger.LogException(new ElympicsException("Failed to perform initial synchronization", e));
                throw;
            }

            InvokeSafely(ConnectedAndSynchronized, synchronizationData, logger);
            _clientSynchronizer.StartContinuousSynchronizingAsync(sessionToken, _clientDisconnectedCts.Token).Forget();
        }

        protected abstract UniTask ConnectInternalAsync(CancellationToken ct = default);

        private static void InvokeSafely(Action? action, ApplicationState logger)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }

        private static void InvokeSafely<T>(Action<T>? action, T arg, ApplicationState logger)
        {
            try
            {
                action?.Invoke(arg);
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }

        protected abstract INetworkClient CreateNetworkClient();

        private void InitializeNetworkClient(INetworkClient networkClient)
        {
            if (_clientDisconnectedCts == null)
                throw new InvalidOperationException();
            networkClient.DataReceived += OnInGameDataReceived;
            networkClient.Disconnected += Disconnect;
            _ = _clientDisconnectedCts.Token.Register(networkClient.Disconnect);
        }

        private void OnTimeout()
        {
            var log = _logger.WithMethodName();
            log.LogError("Synchronize timed out, disconnecting...");
            Disconnect();
        }

        public void Disconnect()
        {
            var cts = _clientDisconnectedCts;
            _clientDisconnectedCts = null;
            if (cts == null)
                return;
            var logger = _logger.WithMethodName();
            logger.LogInfo("Aborting connection.");
            cts.Cancel();
            cts.Dispose();
            _clientSynchronizer.TimedOut -= OnTimeout;
        }

        private void SendReliableCommand(Command command) => NetworkClient?.ReliableChannel.Send(_serializer.Serialize(command));

        private void OnInGameDataReceived(string label, byte[] data)
        {
            try
            {
                var message = _serializer.Deserialize<Message>(data);
                OnMessageDataReceived(label, data, message.Type);
            }
            catch (Exception e)
            {
                var log = _logger.WithMethodName();
                log.LogException(new ElympicsException($"Error in {GetType().Name} receiving a message using channel {label}", e));
            }
        }

        private void OnMessageDataReceived(string label, byte[] data, MessageType type)
        {
            switch (type)
            {
                case MessageType.Connected:
                    _ = InvokeReceivedMessageEvent<ConnectedMessage>(data, OnSessionConnected);
                    break;
                case MessageType.PingServer:
                    RespondForPing();
                    break;
                case MessageType.PingClientResponse when label == INetworkClient.ReliableLabel:
                    _clientSynchronizer.ReliablePingReceived(_serializer.Deserialize<PingClientResponseMessage>(data));
                    break;
                case MessageType.PingClientResponse when label == INetworkClient.UnreliableLabel:
                    _clientSynchronizer.UnreliablePingReceived(_serializer.Deserialize<PingClientResponseMessage>(data));
                    break;
                case MessageType.InGameData:
                    _ = InvokeReceivedMessageEvent<InGameDataMessage>(data, InvokeDataReceived);
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
                case MessageType.UnknownCommandMessage:
                    ElympicsLogger.LogError(_serializer.Deserialize<UnknownCommandMessage>(data).ErrorMessage);
                    break;
                case MessageType.PingClientResponse:
                case MessageType.None:
                default:
                    break;
            }

            void InvokeDataReceived(InGameDataMessage dataMessage)
            {
                InGameDataReceived?.Invoke(label, dataMessage);
            }

            void OnSessionConnected(ConnectedMessage message)
            {
                _ = _sessionConnectedTcs?.TrySetResult(message);
            }
        }

        private void RespondForPing() => SendReliableCommand(new PingServerResponseCommand());

        private T InvokeReceivedMessageEvent<T>(byte[] data, Action<T>? action)
        {
            var message = _serializer.Deserialize<T>(data);
            if (message != null)
                action?.Invoke(message);
            return message;
        }

        public void AuthenticateMatchUserSecretAsync(string userSecret) =>
            SendReliableCommand(new AuthenticateMatchUserSecretCommand { UserSecret = userSecret });

        public void AuthenticateAsSpectatorAsync() =>
            SendReliableCommand(new AuthenticateAsSpectatorCommand());

        public void JoinMatchAsync() =>
            SendReliableCommand(new JoinMatchCommand());

        public void SendInGameDataReliable(byte[] data) =>
            SendReliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });

        public void SendInGameDataUnreliable(byte[] data) =>
            SendUnreliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });

        private void SendUnreliableCommand(object command) =>
            NetworkClient?.UnreliableChannel.Send(_serializer.Serialize(command));
    }
}
