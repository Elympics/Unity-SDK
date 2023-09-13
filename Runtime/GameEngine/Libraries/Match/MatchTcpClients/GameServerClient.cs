using System;
using System.Threading;
using System.Threading.Tasks;
using Elympics;
using MatchTcpClients.Synchronizer;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpModels.Commands;
using MatchTcpModels.Messages;

namespace MatchTcpClients
{
    public abstract class GameServerClient : IGameServerClient
    {
        private protected readonly GameServerClientConfig Config;

        private readonly IGameServerSerializer _serializer;

        public bool IsConnected => ReliableClient?.IsConnected ?? false;
        public bool IsUnreliableConnected => UnreliableClient?.IsConnected ?? false;
        public string SessionToken { get; private set; }

        private protected IReliableNetworkClient ReliableClient;
        private protected IUnreliableNetworkClient UnreliableClient;
        private protected CancellationTokenSource ClientDisconnectedCts;

        private IClientSynchronizer _clientSynchronizer;

        public event Action Connected;
        public event Action<TimeSynchronizationData> ConnectedAndSynchronized;
        public event Action<TimeSynchronizationData> Synchronized;
        public event Action Disconnected;
        public event Action<UserMatchAuthenticatedMessage> UserMatchAuthenticated;
        public event Action<AuthenticatedAsSpectatorMessage> AuthenticatedAsSpectator;
        public event Action<MatchJoinedMessage> MatchJoined;
        public event Action<MatchEndedMessage> MatchEnded;
        public event Action<InGameDataMessage> InGameDataReliableReceived;
        public event Action<InGameDataMessage> InGameDataUnreliableReceived;

        private protected event Action<ConnectedMessage> SessionConnected;

        protected GameServerClient(
            IGameServerSerializer serializer,
            GameServerClientConfig config)
        {
            ElympicsLogger.Log($"Game server client of type {GetType().Name} will be used.");

            _serializer = serializer;
            Config = config;
        }

        public async Task<bool> ConnectAsync(CancellationToken ct = default)
        {
            Disconnect();

            CreateNetworkClients();

            _clientSynchronizer = new ClientSynchronizer(Config.ClientSynchronizerConfig);
            ClientDisconnectedCts = new CancellationTokenSource();

            InitializeNetworkClients();
            InitClientSynchronizer();

            ReliableClient.CreateAndBind();
            UnreliableClient.CreateAndBind();

            if (!await ConnectInternalAsync(ct))
                return false;

            Connected?.Invoke();
            InitClientDisconnectedCts();
            if (!IsConnected)
            {
                Disconnect();
                return false;
            }

            var synchronizationData = await TryInitialSynchronizeAsync(ct);
            if (synchronizationData == null)
            {
                Disconnect();
                return false;
            }

            ConnectedAndSynchronized?.Invoke(synchronizationData);
            _ = _clientSynchronizer.StartContinuousSynchronizingAsync(ClientDisconnectedCts.Token).ConfigureAwait(false);
            return true;
        }

        protected abstract void CreateNetworkClients();

        protected virtual void InitializeNetworkClients()
        {
            InitReliableClient();
            InitUnreliableClient();
        }

        protected abstract Task<bool> ConnectInternalAsync(CancellationToken ct = default);

        protected abstract Task<bool> TryInitializeSessionAsync(CancellationToken ct = default);

        private protected async Task<bool> TryConnectSessionAsync(CancellationToken ct = default)
        {
            var sessionConnectedCompletionSource = new TaskCompletionSource<bool>();

            void OnSessionConnected(ConnectedMessage message)
            {
                ElympicsLogger.Log("Connected using reliable channel.");
                SessionToken = message.SessionToken;
                _clientSynchronizer.SetUnreliableSessionToken(message.SessionToken);
                sessionConnectedCompletionSource.SetResult(true);
            }

            SessionConnected += OnSessionConnected;

            try
            {
                if (!await TryInitializeSessionAsync(ct))
                    return false;

                ElympicsLogger.Log("Establishing connection...");
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var sessionConnectedCompletionTask = sessionConnectedCompletionSource.Task;
                var timeoutTask = TaskUtil.Delay(Config.SessionConnectTimeout, cts.Token).CatchOperationCanceledException();

                if (await Task.WhenAny(sessionConnectedCompletionTask, timeoutTask) != timeoutTask)
                {
                    cts.Cancel();
                    return true;
                }

                ElympicsLogger.LogError("Could not establish the connection.");
                return false;
            }
            finally
            {
                SessionConnected -= OnSessionConnected;
            }
        }

        private async Task<TimeSynchronizationData> TryInitialSynchronizeAsync(CancellationToken ct = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, ClientDisconnectedCts.Token);

            TimeSynchronizationData data = null;
            for (var i = 0; i < Config.InitialSynchronizeMaxRetries; i++)
            {
                data = await _clientSynchronizer.SynchronizeOnce(linkedCts.Token);
                if (data == null)
                    return null;
            }

            return data;
        }

        private void InitReliableClient()
        {
            ReliableClient.DataReceived += OnReliableMessageDataReceived;
            _ = ClientDisconnectedCts.Token.Register(ReliableClient.Disconnect);
        }

        private void InitUnreliableClient()
        {
            UnreliableClient.DataReceived += OnUnreliableMessageDataReceived;
            _ = ClientDisconnectedCts.Token.Register(UnreliableClient.Disconnect);
        }

        private void InitClientDisconnectedCts()
        {
            _ = ClientDisconnectedCts.Token.Register(() =>
            {
                ElympicsLogger.Log("Client disconnected.");
                Disconnected?.Invoke();
            });

            ReliableClient.Disconnected += Disconnect;
        }

        private void InitClientSynchronizer()
        {
            _clientSynchronizer.ReliablePingGenerated += async command => await SendReliableCommand(command);
            _clientSynchronizer.UnreliablePingGenerated += async command => await SendUnreliableCommand(command);
            _clientSynchronizer.AuthenticateUnreliableGenerated += async command => await SendUnreliableCommand(command);
            _clientSynchronizer.Synchronized += data => Synchronized?.Invoke(data);
            _clientSynchronizer.TimedOut += () =>
            {
                ElympicsLogger.Log("Synchronize timed out, disconnecting...");
                Disconnect();
            };
        }

        public void Disconnect()
        {
            ElympicsLogger.Log("Aborting connection.");
            ClientDisconnectedCts?.Cancel();
            ClientDisconnectedCts = null;
        }

        private async Task SendReliableCommand(Command command)
        {
            _ = await ReliableClient.SendAsync(_serializer.Serialize(command));
        }

        private void OnReliableMessageDataReceived(byte[] data)
        {
            try
            {
                var message = _serializer.Deserialize<Message>(data);
                OnReliableMessageDataReceived(data, message.Type);
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException($"Error in {GetType().Name} receiving a message using reliable channel", e);
            }
        }

        private void OnReliableMessageDataReceived(byte[] data, MessageType type)
        {
            switch (type)
            {
                case MessageType.Connected:
                    _ = InvokeReceivedMessageEvent(data, SessionConnected);
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
                    break;
                default:
                    break;
            }
        }

        private void RespondForPing()
        {
            _ = SendReliableCommand(new PingServerResponseCommand());
        }

        private T InvokeReceivedMessageEvent<T>(byte[] data, Action<T> action)
        {
            var message = _serializer.Deserialize<T>(data);
            if (message != null)
                action?.Invoke(message);
            return message;
        }

        public async Task AuthenticateMatchUserSecretAsync(string userSecret) =>
            await SendReliableCommand(new AuthenticateMatchUserSecretCommand { UserSecret = userSecret });
        public async Task AuthenticateAsSpectatorAsync() =>
            await SendReliableCommand(new AuthenticateAsSpectatorCommand());
        public async Task JoinMatchAsync() =>
            await SendReliableCommand(new JoinMatchCommand());

        public async Task SendInGameDataReliableAsync(byte[] data) =>
            await SendReliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });
        public async Task SendInGameDataUnreliableAsync(byte[] data) =>
            await SendUnreliableCommand(new InGameDataCommand { Data = Convert.ToBase64String(data) });

        private async Task SendUnreliableCommand(object command) =>
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
                _ = ElympicsLogger.LogException($"Error in {GetType().Name} receiving a message using unreliable channel", e);
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
                    break;
                case MessageType.PingServer:
                    break;
                case MessageType.MatchJoined:
                    break;
                case MessageType.UserMatchAuthenticatedMessage:
                    break;
                case MessageType.MatchEnded:
                    break;
                case MessageType.AuthenticateAsSpectator:
                    break;
                default:
                    break;
            }
        }
    }
}
