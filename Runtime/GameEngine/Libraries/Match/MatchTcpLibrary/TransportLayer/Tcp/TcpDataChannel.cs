#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;

#pragma warning disable CS0067 // Error is part of IDataChannel for WebRTC channels; TCP surfaces failures via exceptions instead

namespace MatchTcpLibrary.TransportLayer.Tcp
{
    public class TcpDataChannel : IDataChannel
    {
        public event Action? Disconnected;
        public event Action<byte[]>? DataReceived;
        public event Action<string>? Error;

        public string Label { get; }
        private static readonly IPEndPoint AnyEndPoint = new(IPAddress.Any, 0);

        private readonly IMessageEncoder _messageEncoder;

        private readonly TcpProtocolConfig _tcpProtocolConfig;
        private CancellationTokenSource? _connectingTokenSource;

        private TcpClient? _tcpClient;
        private TcpReceiver? _tcpReceiver;
        private readonly List<byte> _receivedBytes;

        private readonly object _connectingLock = new();
        private bool _connecting;

        private readonly object _isConnectedLock = new();
        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                var isDisconnected = false;
                lock (_isConnectedLock)
                {
                    if (_isConnected != value && !value)
                        isDisconnected = true;
                    _isConnected = value;
                }

                if (isDisconnected)
                    Disconnected?.Invoke();
            }
        }

        public TcpDataChannel(string label, IMessageEncoder messageEncoder, TcpProtocolConfig tcpProtocolConfig, TcpClient? client = null)
        {
            Label = label;
            _messageEncoder = messageEncoder;
            _tcpProtocolConfig = tcpProtocolConfig;
            _connectingTokenSource = new CancellationTokenSource();
            _tcpClient = client;
            _receivedBytes = new List<byte>();

            if (_tcpClient != null)
                SetupTcpClient(_tcpClient);
        }

        public void CreateAndBind()
        {
            Disconnect();
            _tcpClient = new TcpClient();
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _tcpClient.Client.Bind(AnyEndPoint);
            SetupTcpClient(_tcpClient);
        }

        private void SetupTcpClient(TcpClient tcpClient)
        {
            _tcpReceiver = new TcpReceiver(tcpClient, _tcpProtocolConfig);
            _tcpReceiver.DataReceived += OnDataReceived;
            _tcpReceiver.ReceivingStopped += OnReceivingStopped;

            IsConnected = tcpClient.Connected;
            if (IsConnected)
                StartReceiving();
        }

        private void OnDataReceived(byte[] data)
        {
            _receivedBytes.AddRange(data);
            try
            {
                ExtractMessages();
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException($"{nameof(TcpDataChannel)} failed to process a message", e);
            }
        }

        private void ExtractMessages()
        {
            var trimmedData = _messageEncoder.ExtractCompleteMessages(_receivedBytes);
            foreach (var data in trimmedData)
                DataReceived?.Invoke(data.ToArray());
        }

        private void OnReceivingStopped()
        {
            ElympicsLogger.Log($"{GetType().Name} stopped receiving, disconnecting...");
            Disconnect();
        }

        private void StartReceiving()
        {
            if (_tcpReceiver is null)
                throw new InvalidOperationException("TCP receiver has not been created");
            _tcpReceiver.StartReceiving().Forget();
        }

        public async UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default)
        {
            try
            {
                if (CheckIfConnectingAndSet())
                    throw ElympicsLogger.LogException(new InvalidOperationException("Connection already in progress"));

                if (NotCreated())
                    throw ElympicsLogger.LogException(new InvalidOperationException($"{nameof(CreateAndBind)} has not been called before connecting"));
                else
                    RecreateSocket();

                await TryConnectAsync(remoteEndPoint, ct);

                if (!IsConnected)
                    throw ElympicsLogger.LogException(new ElympicsException("Could not connect"));

                StartReceiving();
            }
            finally
            {
                SetConnectingFalse();
            }
        }

        private bool CheckIfConnectingAndSet()
        {
            lock (_connectingLock)
            {
                if (_connecting)
                    return true;
                _connecting = true;
                return false;
            }
        }

        private void SetConnectingFalse()
        {
            lock (_connectingLock)
                _connecting = false;
        }


        private bool NotCreated() => _tcpClient == null;

        private void RecreateSocket() => CreateAndBind();

        private async UniTask TryConnectAsync(IPEndPoint endpoint, CancellationToken ct = default)
        {
            if (_tcpClient is null)
                throw new InvalidOperationException("TCP client has not been created");
            _connectingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            for (var i = 0; i < _tcpProtocolConfig.MaxConnectionAttempts; i++)
            {
                var result = await ConnectSingleAsync(endpoint, _connectingTokenSource.Token);
                switch (result)
                {
                    case ConnectResult.Connected:
                        IsConnected = true;
                        break;
                    case ConnectResult.TimedOut:
                        break;
                    case ConnectResult.TimedOutButConnectedError:
                        RecreateSocket();
                        // Previous cts is bounded to this call of _tcpClient.Connect(), and cancelled using RecreateSocket() ~pprzestrzelski 20.01.2020
                        _connectingTokenSource = new CancellationTokenSource();
                        break;
                    case ConnectResult.OtherException:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                if (IsConnected || _connectingTokenSource.IsCancellationRequested)
                    break;

                ElympicsLogger.Log($"Connection attempt no. {i} failed on {endpoint}, reason {result}, retrying...");

                await UniTask.Delay(_tcpProtocolConfig.IntervalBetweenConnectionAttemptsInMs,
                    DelayType.Realtime, cancellationToken: _connectingTokenSource.Token);
            }

            IsConnected = _tcpClient.Connected;
        }

        private async UniTask<ConnectResult> ConnectSingleAsync(IPEndPoint endPoint, CancellationToken ct = default)
        {
            if (_tcpClient is null)
                throw new InvalidOperationException("TCP client has not been created");
            try
            {
                await _tcpClient.ConnectAsync(endPoint.Address, endPoint.Port).AsUniTask()
                    .WithTimeout(TimeSpan.FromMilliseconds(_tcpProtocolConfig.ConnectTimeoutMs), ct);
                return ConnectResult.Connected;
            }
            catch (TimeoutException)
            {
                // It sometimes happens (from time to time), fix when Microsoft adds cts handling in ConnectAsync ~pprzestrzelski 20.01.2020
                return _tcpClient.Connected ? ConnectResult.TimedOutButConnectedError : ConnectResult.TimedOut;
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException($"{nameof(TcpDataChannel)} connection exception", e);
                return ConnectResult.OtherException;
            }
        }

        private enum ConnectResult
        {
            Connected,
            TimedOut,
            TimedOutButConnectedError,
            OtherException
        }

        public void Send(byte[] dataToSend)
        {
            if (!IsConnected)
                throw ElympicsLogger.LogException(new InvalidOperationException("Not connected"));
            if (_tcpClient is null)
                throw new InvalidOperationException("TCP client has not been created");

            var bytes = _messageEncoder.EncodePayload(dataToSend);
            try
            {
                _tcpClient.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch
            {
                ElympicsLogger.LogError($"{GetType().Name} failed to send a message, disconnecting...");
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            _connectingTokenSource?.Cancel();
            _connectingTokenSource = null;
            _tcpClient?.Close();
            _tcpClient = null;
            _tcpReceiver?.StopReceiving();
            _tcpReceiver = null;
            IsConnected = false;
        }

        public void Dispose() => Disconnect();
    }
}
