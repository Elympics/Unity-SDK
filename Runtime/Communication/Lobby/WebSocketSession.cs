using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby.Models;
using Elympics.Lobby.Serializers;
using HybridWebSocket;
using UnityEngine;
using Ping = Elympics.Lobby.Models.Ping;

#nullable enable

namespace Elympics.Lobby
{
    internal class WebSocketSession : IWebSocketSessionInternal, IWebSocketSession, IDisposable
    {
        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<IFromLobby>? MessageReceived;

        public TimeSpan OpeningTimeout = TimeSpan.FromSeconds(5);
        public TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _automaticDisconnectThreshold = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<Guid, Action<OperationResult>> _operationResultHandlers = new();

        private IWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private TimeoutController _timeoutController;
        private CancellationToken Token => _cts?.Token ?? new CancellationToken(true);

        private bool _isDisposed;

        private readonly IAsyncEventsDispatcher _dispatcher;
        public delegate IWebSocket WebSocketFactory(string url, string? protocol = null);
        private readonly WebSocketFactory _wsFactory;

        private readonly ILobbySerializer _serializer;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                _dispatcher.Enqueue(value ? () => Connected?.Invoke() : () => Disconnected?.Invoke());
            }
        }

        private bool _isConnected;

        public SessionConnectionDetails ConnectionDetails =>
            IsConnected ? _connectionDetails!.Value : throw new InvalidOperationException($"{nameof(WebSocketSession)} is not connected.");

        private SessionConnectionDetails? _connectionDetails;

        public WebSocketSession(IAsyncEventsDispatcher dispatcher, WebSocketFactory? wsFactory = null, ILobbySerializer? serializer = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _wsFactory = wsFactory ?? HybridWebSocket.WebSocketFactory.CreateInstance;
            _serializer = serializer ?? new MessagePackLobbySerializer();
        }

        public async UniTask Connect(SessionConnectionDetails details, CancellationToken ct = default)
        {
            var (wsUrl, authData, gameId, gameVersion, regionName) = details;
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (_cts is not null)
                throw ElympicsLogger.LogException(new InvalidOperationException("Connecting already in progress."));
            _cts = new CancellationTokenSource();
            _timeoutController = new TimeoutController(_cts, DelayType.Realtime);
            var (url, protocol) = wsUrl.ToWebSocketAddress(authData.JwtToken);
            _ws = _wsFactory(url, protocol);
            _ws.OnError += HandleError;
            _ws.OnClose += HandleClose;
            _ws.OnMessage += HandleMessage;
            using var ctr = ct.RegisterWithoutCaptureExecutionContext(Disconnect);
            try
            {
                await OpenWebSocket(_ws);
                await EstablishSession(gameId, gameVersion, regionName);
                _connectionDetails = details;
                IsConnected = true;
                AutoDisconnectOnTimeout().Forget();
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested)
                    throw new LobbyOperationException("Disconnected while trying to establish session");
                throw;
            }
        }

        public void Disconnect()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            IsConnected = false;
            if (_cts is null)
                return;
            _cts.Cancel();
            _cts.Dispose();
            _timeoutController.Dispose();
            _cts = null;
            _operationResultHandlers.Clear();
            ResetWebSocket();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            Disconnect();
            Connected = null;
            Disconnected = null;
            MessageReceived = null;
            _isDisposed = true;
        }

        public async UniTask<OperationResult> ExecuteOperation(LobbyOperation message, CancellationToken ct = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (!IsConnected)
                throw ElympicsLogger.LogException(new InvalidOperationException("Cannot send message before establishing the WebSocket "));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(Token, ct);
            SendMessage(message);
            var result = await WaitForOperationResult(message.OperationId, OperationTimeout, linkedCts.Token);
            return result;
        }

        private async UniTask OpenWebSocket(IWebSocket ws)
        {
            var openTask = ResultUtils.WaitForResult<ValueTuple, WebSocketOpenEventHandler>(OpeningTimeout, tcs => () => tcs.TrySetResult(new ValueTuple()), handler => ws.OnOpen += handler, handler => ws.OnOpen -= handler, Token);
            ws.Connect();
            _ = await openTask;
        }

        private async UniTask EstablishSession(Guid gameId, string gameVersion, string regionName)
        {
            var request = new JoinLobby(ElympicsConfig.SdkVersion, gameId, gameVersion, regionName);
            var ackTask = WaitForOperationResult(request.OperationId, OperationTimeout, Token);
            SendMessage(request);
            _ = await ackTask;
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
            catch
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
                        ElympicsLogger.Log($"Sending WebSocket message: {typeName} to: {_connectionDetails?.Url}\n{representation}");
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
                var message = _serializer.Deserialize(data);
#if ELYMPICS_DEBUG
                {
                    var typeName = message.GetType().FullName;
                    if (_serializer.TryGetHumanReadableRepresentation(data, out var representation))
                        ElympicsLogger.Log($"Received WebSocket message: {typeName} from: {_connectionDetails?.Url}\n{representation}");
                }
#endif
                if (message is Ping)
                {
                    ResetTimeout();
                    SendMessage(new Pong());
                    return;
                }

                if (message is OperationResult result)
                {
                    if (_operationResultHandlers.TryRemove(result.OperationId, out var resultHandler))
                        DispatchWithCancellation(() => resultHandler.Invoke(result));
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
            Disconnect();
            _dispatcher.Enqueue(() => ElympicsLogger.LogError(message));
        }

        private void HandleClose(WebSocketCloseCode code, string reason)
        {
            _dispatcher.Enqueue(code != WebSocketCloseCode.Normal ? () => ElympicsLogger.LogError($"Connection closed abnormally [{code}] {reason}") : () => ElympicsLogger.Log($"Connection closed gracefully [{code}] {reason}"));

            if (IsConnected)
                Reconnect().Forget();
            else
                Disconnect();
        }
        private async UniTask Reconnect()
        {
            Disconnect();
            _dispatcher.Enqueue(() => ElympicsLogger.Log("Reconnecting to lobby..."));
            try
            {
                await Connect(_connectionDetails!.Value);
                _dispatcher.Enqueue(() => ElympicsLogger.Log("Reconnected."));
            }
            catch (Exception e)
            {
                _dispatcher.Enqueue(() => _ = ElympicsLogger.LogException(e));
            }
        }
        private async UniTaskVoid AutoDisconnectOnTimeout()
        {
            await UniTask.WaitUntilCanceled(cancellationToken: _timeoutController.Timeout(_automaticDisconnectThreshold));
            if (_isConnected)
                Disconnect();
        }
        private void ResetTimeout() => _ = _timeoutController.Timeout(_automaticDisconnectThreshold);
        private UniTask<OperationResult> WaitForOperationResult(Guid operationId, TimeSpan timeout, CancellationToken ct) =>
            ResultUtils.WaitForResult<OperationResult, Action<OperationResult>>(timeout, tcs => result => _ = result.Success ? tcs.TrySetResult(result) : tcs.TrySetException(new LobbyOperationException(result)), handler => _operationResultHandlers.TryAdd(operationId, handler), _ => _operationResultHandlers.TryRemove(operationId, out var _), ct);

        private void DispatchWithCancellation(Action action) =>
            _dispatcher.Enqueue(action.WithCancellation(Token));
    }
}
