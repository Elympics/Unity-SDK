using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using Elympics.Communication.Utils;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby.Serializers;
using HybridWebSocket;

#nullable enable

namespace Elympics.Lobby
{
    internal class WebSocketSession : IWebSocketSessionInternal, IWebSocketSession, IDisposable
    {
        public event Action? Connected;
        public event Action<DisconnectionData>? Disconnected;
        public event Action<IFromLobby>? MessageReceived;

        private readonly ConcurrentDictionary<Guid, Action<OperationResultDto>> _operationResultHandlers = new();

        private readonly ConcurrentDictionary<Guid, Action<ILobbyResponse>> _dataResponses = new();

        private IWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private CancellationToken Token => _cts?.Token ?? new CancellationToken(true);

        private bool _isDisposed;

        private readonly IAsyncEventsDispatcher _dispatcher;
        private ElympicsLoggerContext _logger;
        public delegate IWebSocket WebSocketFactory(string url, string? protocol = null);
        private readonly WebSocketFactory _wsFactory;

        private readonly ILobbySerializer _serializer;
        private Stopwatch? _timer;
        private readonly IWebSocketSessionController _controller;

        public bool IsConnected { get; private set; }

        public SessionConnectionDetails? ConnectionDetails { get; private set; }

        public WebSocketSession(
            IWebSocketSessionController controller,
            IAsyncEventsDispatcher dispatcher,
            ElympicsLoggerContext logger,
            WebSocketFactory? wsFactory = null,
            ILobbySerializer? serializer = null)
        {
            _controller = controller;
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger.WithContext(nameof(WebSocketSession));
            _wsFactory = wsFactory ?? HybridWebSocket.WebSocketFactory.CreateInstance;
            _serializer = serializer ?? new MessagePackLobbySerializer();
        }

        public async UniTask<GameDataResponseDto> Connect(SessionConnectionDetails details, CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            var (wsUrl, authData, gameId, gameVersion, regionName) = details;
            if (_isDisposed)
                throw logger.CaptureAndThrow(new ObjectDisposedException(GetType().FullName));
            if (_cts is not null)
                throw logger.CaptureAndThrow(new InvalidOperationException("Connecting already in progress."));
            _cts = new CancellationTokenSource();
            var (url, protocol) = wsUrl.ToWebSocketAddress(authData.JwtToken);
            _ws = _wsFactory(url, protocol);
            _ws.OnError += HandleError;
            _ws.OnClose += HandleClose;
            _ws.OnMessage += HandleMessage;
            using var ctr = ct.RegisterWithoutCaptureExecutionContext(() => DisconnectInternal(DisconnectionReason.Unknown));
            try
            {
                await OpenWebSocket(_ws);
                var gameData = await SendRequestInternal<GameDataResponseDto>(new JoinLobbyDto(ElympicsConfig.SdkVersion, gameId, gameVersion, regionName), Token);
                ConnectionDetails = details;
                logger.SetRegion(regionName).SetLobbyUrl(wsUrl).Log("Connection to lobby completed.");
                SetConnectedState();
                _timer = new Stopwatch();
                _timer.Start();
                AutoDisconnectOnTimeout(_cts.Token).Forget();
                return gameData;
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested)
                    throw _logger.CaptureAndThrow(new LobbyOperationException("Disconnected while trying to establish session"));
                throw;
            }
        }
        public void Disconnect(DisconnectionReason reason)
        {
            CleanupSession();
            SetDisconnectedState();
            var data = new DisconnectionData(DisconnectionReason.ClientRequest);
            Disconnected?.Invoke(data);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            DisconnectInternal(DisconnectionReason.ApplicationShutdown);
            Connected = null;
            Disconnected = null;
            MessageReceived = null;
            _isDisposed = true;
        }

        public async UniTask<OperationResultDto> ExecuteOperation(LobbyOperation message, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            return await ExecuteOperationInternal(message, ct);
        }

        private async UniTask<OperationResultDto> ExecuteOperationInternal(LobbyOperation message, CancellationToken ct)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(Token, ct);
            var task = WaitForOperationResult(message.OperationId, ElympicsTimeout.WebSocketOperationTimeout, $"{nameof(ExecuteOperationInternal)} {message.GetType().Name} with {nameof(message.OperationId)}: {message.OperationId}", linkedCts.Token);
            SendMessage(message);
            return await task;
        }

        public async UniTask<TResponse> SendRequest<TResponse>(LobbyOperation message, CancellationToken ct = default)
            where TResponse : ILobbyResponse
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            return await SendRequestInternal<TResponse>(message, ct);
        }

        private async UniTask<TResponse> SendRequestInternal<TResponse>(LobbyOperation message, CancellationToken ct)
            where TResponse : ILobbyResponse
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(Token, ct);
            var dataTask = WaitForLobbyData<TResponse>(message.OperationId, ElympicsTimeout.WebSocketOperationTimeout, linkedCts.Token);
            try
            {
                _ = await ExecuteOperationInternal(message, linkedCts.Token); //If the result was an error, this will throw an exception
            }
            catch
            {
                try
                {
                    //A response will never arrive if we got an error as the result, so cancel the operation and suppress the OperationCanceledException exception
                    linkedCts.Cancel();
                    _ = await dataTask.SuppressCancellationThrow();
                }
                catch (Exception exception)
                {
                    _ = ElympicsLogger.LogException(exception); //If there is an exception unrelated to cancellation, log it
                }

                throw; //Throw the exception that contains the result's error message
            }

            return await dataTask;
        }

        #region private

        private async UniTask OpenWebSocket(IWebSocket ws)
        {
            var openTask = ResultUtils.WaitForResult<ValueTuple, WebSocketOpenEventHandler>(ElympicsTimeout.WebSocketOpeningTimeout,
                tcs => () => tcs.TrySetResult(new ValueTuple()),
                nameof(OpenWebSocket),
                handler => ws.OnOpen += handler,
                handler => ws.OnOpen -= handler,
                Token);
            ws.Connect();
            _ = await openTask;
        }

        private void ResetWebSocket()
        {
            if (_ws is null)
                return;
            _ws.OnMessage -= HandleMessage;
            _ws.OnError -= HandleError;
            _ws.OnClose -= HandleClose;
            try
            {
                _ws.Close();
            }
            catch (Exception)
            { /* ignored */
            }
            _ws = null;
        }

        private void SendMessage(IToLobby message)
        {
            try
            {
                var data = _serializer.Serialize(message);
#if ELYMPICS_DEBUG
                {
                    var typeName = message.GetType().FullName;
                    if (_serializer.TryGetHumanReadableRepresentation(data, out var representation))
                        ElympicsLogger.Log($"Sending WebSocket message: {typeName} to: {ConnectionDetails?.Url}\n{representation}");
                }
#endif
                _ws?.Send(data);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private void HandleMessage(byte[] data)
        {
            try
            {
                var message = _serializer.Deserialize(data)
                    ?? throw new ElympicsException("Invalid message received");
#if ELYMPICS_DEBUG
                {
                    var typeName = message.GetType().FullName;
                    if (_serializer.TryGetHumanReadableRepresentation(data, out var representation))
                        ElympicsLogger.Log($"Received WebSocket message: {typeName} from: {ConnectionDetails?.Url}\n{representation}");
                }
#endif
                if (message is PingDto)
                {
                    DispatchWithCancellation(() =>
                    {
                        _timer?.Reset();
                        _timer?.Start();
                    });
                    SendMessage(new PongDto());
                    return;
                }
                if (message is OperationResultDto result)
                {
                    if (_operationResultHandlers.TryRemove(result.OperationId, out var resultHandler))
                        DispatchWithCancellation(() => resultHandler.Invoke(result));
                    return;
                }
                if (message is ILobbyResponse dataFromLobby)
                {
                    if (_dataResponses.TryRemove(dataFromLobby.RequestId, out var requestHandler))
                        DispatchWithCancellation(() => requestHandler.Invoke(dataFromLobby));
                    return;
                }
                DispatchWithCancellation(() => MessageReceived?.Invoke(message));
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }
        private void HandleError(string message)
        {
            DisconnectInternal(DisconnectionReason.Error);
            _dispatcher.Enqueue(() => ElympicsLogger.LogError(message));
        }

        private void HandleClose(WebSocketCloseCode code, string reason)
        {
            var logger = _logger.WithMethodName();
            _dispatcher.Enqueue(code != WebSocketCloseCode.Normal ? () => logger.Error($"Connection closed abnormally [{code}] {reason}")
                : () => logger.Log($"Connection closed gracefully [{code}] {reason}"));

            DisconnectInternal(IsConnected && code == WebSocketCloseCode.Away ? DisconnectionReason.Timeout : DisconnectionReason.Closed);
        }

        private async UniTaskVoid AutoDisconnectOnTimeout(CancellationToken cancellationToken) => await UniTask.RunOnThreadPool(() =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_timer is null)
                        return;

                    if (!(_timer.Elapsed.TotalSeconds > ElympicsTimeout.WebSocketHeartbeatTimeout.TotalSeconds))
                        continue;

                    if (!IsConnected)
                        return;

                    var logger = _logger.WithMethodName();
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    logger.Error("We have not received a response from the server. You have been disconnected. Please check your internet connection and try reconnecting.");
                    DisconnectInternal(DisconnectionReason.Closed);
                    return;
                }
            },
            true,
            cancellationToken);

        private void DisconnectInternal(DisconnectionReason reason)
        {
            CleanupSession();
            SetDisconnectedState();
            _dispatcher.Enqueue(() => PropagateDisconnection(reason).Forget());
        }

        private void CleanupSession()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (_cts is null)
                return;
            _timer?.Stop();
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            _operationResultHandlers.Clear();
            ResetWebSocket();
        }

        private void SetConnectedState()
        {
            if (IsConnected)
                return;

            IsConnected = true;
            _dispatcher.Enqueue(() => Connected?.Invoke());
        }

        private void SetDisconnectedState()
        {
            if (!IsConnected)
                return;
            IsConnected = false;
        }

        private async UniTask PropagateDisconnection(DisconnectionReason reason)
        {
            var data = new DisconnectionData(reason);
            await _controller.ReconnectIfPossible(data);
            if (!IsConnected)
                Disconnected?.Invoke(data);
        }

        private UniTask<OperationResultDto> WaitForOperationResult(Guid operationId, TimeSpan timeout, string operationName, CancellationToken ct) =>
            ResultUtils.WaitForResult<OperationResultDto, Action<OperationResultDto>>(timeout,
                tcs => result => _ = result.Success ? tcs.TrySetResult(result) : tcs.TrySetException(new LobbyOperationException(result)),
                operationName,
                handler => _operationResultHandlers.TryAdd(operationId, handler),
                _ => _operationResultHandlers.TryRemove(operationId, out var _),
                ct);

        private async UniTask<TData> WaitForLobbyData<TData>(Guid requestId, TimeSpan timeout, CancellationToken ct)
            where TData : ILobbyResponse
        {
            var result = await ResultUtils.WaitForResult<IFromLobby, Action<IFromLobby>>(timeout,
                tcs => result => _ = tcs.TrySetResult(result),
                $"{nameof(WaitForLobbyData)} {nameof(TData)} with {nameof(requestId)}: {requestId}",
                handler => _dataResponses.TryAdd(requestId, handler),
                _ => _dataResponses.TryRemove(requestId, out var _),
                ct);
            return (TData)result;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void ThrowIfNotConnected()
        {
            if (IsConnected)
                return;
            var logger = _logger.WithMethodName();
            throw logger.CaptureAndThrow(new InvalidOperationException("Cannot send message before establishing the WebSocket "));
        }

        private void DispatchWithCancellation(Action action) =>
            _dispatcher.Enqueue(action.WithCancellation(Token));

        #endregion
    }
}
