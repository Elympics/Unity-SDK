using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.Tcp
{
    public class TcpNetworkClient : IReliableNetworkClient
    {
        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        private readonly IPEndPoint _anyEndPoint = new(IPAddress.Any, 0);
        public IPEndPoint LocalEndPoint => _tcpClient?.Client?.LocalEndPoint as IPEndPoint;
        public IPEndPoint RemoteEndpoint => _tcpClient?.Client?.RemoteEndPoint as IPEndPoint;

        private readonly IMatchTcpLibraryLogger _logger;
        private readonly IMessageEncoder _messageEncoder;

        private readonly TcpProtocolConfig _tcpProtocolConfig;
        private CancellationTokenSource _connectingTokenSource;

        private TcpClient _tcpClient;
        private TcpReceiver _tcpReceiver;
        private readonly List<byte> _receivedBytes;
        private IPEndPoint _previousLocalEndPoint;

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
                    if (_isConnected != value && value == false)
                        isDisconnected = true;
                    _isConnected = value;
                }

                if (isDisconnected)
                    Disconnected?.Invoke();
            }
        }

        public TcpNetworkClient(IMatchTcpLibraryLogger logger, IMessageEncoder messageEncoder, TcpProtocolConfig tcpProtocolConfig,
            TcpClient client = null)
        {
            _messageEncoder = messageEncoder;
            _logger = logger;
            _tcpProtocolConfig = tcpProtocolConfig;
            _connectingTokenSource = new CancellationTokenSource();
            _tcpClient = client;
            _receivedBytes = new List<byte>();

            if (_tcpClient != null)
                SetupUnderlyingTcpClient();
        }

        public void CreateAndBind()
        {
            CreateAndBind(_anyEndPoint);
        }

        public void CreateAndBind(int port)
        {
            CreateAndBind(new IPEndPoint(IPAddress.Any, port));
        }

        public void CreateAndBind(IPEndPoint localEndPoint)
        {
            Disconnect();
            _tcpClient = new TcpClient();
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _tcpClient.Client.Bind(localEndPoint);
            SetupUnderlyingTcpClient();
        }

        private void SetupUnderlyingTcpClient()
        {
            _previousLocalEndPoint = (IPEndPoint)_tcpClient.Client.LocalEndPoint;

            _tcpReceiver = new TcpReceiver(_tcpClient, _tcpProtocolConfig);
            _tcpReceiver.DataReceived += OnDataReceived;
            _tcpReceiver.ReceivingStopped += OnReceivingStopped;

            IsConnected = _tcpClient.Connected;
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
                _logger.Error($"{nameof(TcpNetworkClient)} Processing a message failed: " + e);
            }
        }

        private void ExtractMessages()
        {
            var trimmedData = _messageEncoder.ExtractCompleteMessages(_receivedBytes);
            foreach (var data in trimmedData)
            {
                DataReceived?.Invoke(data.ToArray());
            }
        }

        private void OnReceivingStopped()
        {
            _logger.Debug($"{GetType().Name} receiving stopped, disconnecting...");
            Disconnect();
        }

        private void StartReceiving()
        {
            _ = Task.Run(_tcpReceiver.StartReceiving);
        }

        public async Task<bool> ConnectAsync(IPEndPoint remoteEndPoint)
        {
            try
            {
                if (CheckIfConnectingAndSet())
                    return false;

                if (NotCreated())
                {
                    throw new NullReferenceException("CreateAndBind not called before connecting");
                }
                else if (IsDisconnected() || IsConnected)
                {
                    RecreateSocket();
                }

                await TryConnectAsync(remoteEndPoint);

                if (IsConnected)
                    StartReceiving();

                return IsConnected;
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

        private bool IsDisconnected()
        {
            return _tcpClient == null && _previousLocalEndPoint != null;
        }

        private bool NotCreated()
        {
            return _tcpClient == null && _previousLocalEndPoint == null;
        }

        private void RecreateSocket()
        {
            CreateAndBind(_previousLocalEndPoint);
        }

        private async Task TryConnectAsync(IPEndPoint endpoint)
        {
            _connectingTokenSource = new CancellationTokenSource();
            for (var i = 0; i < _tcpProtocolConfig.MaxConnectionAttempts; i++)
            {
                var result = await ConnectSingleAsync(endpoint, _connectingTokenSource);
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

                _logger.Verbose($"Connecting no. {i} failed on {endpoint}, reason {result}, retrying...");

                await TaskUtil.Delay(_tcpProtocolConfig.IntervalBetweenConnectionAttemptsInMs,
                    _connectingTokenSource.Token);
            }

            IsConnected = _tcpClient.Connected;
        }

        private async Task<ConnectResult> ConnectSingleAsync(IPEndPoint endPoint, CancellationTokenSource cts)
        {
            try
            {
                await _tcpClient.ConnectAsync(endPoint.Address, endPoint.Port)
                    .WithTimeout(TimeSpan.FromMilliseconds(_tcpProtocolConfig.ConnectTimeoutMs), cts);
                return ConnectResult.Connected;
            }
            catch (TimeoutException)
            {
                // It sometimes happens (from time to time), fix when microsoft adds cts handling in ConnectAsync ~pprzestrzelski 20.01.2020
                return _tcpClient.Connected ? ConnectResult.TimedOutButConnectedError : ConnectResult.TimedOut;
            }
            catch (Exception e)
            {
                _logger.Error($"{nameof(TcpNetworkClient)} connect exception", e);
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

        public async Task<bool> SendAsync(byte[] dataToSend)
        {
            if (!IsConnected)
                return false;

            var bytes = _messageEncoder.EncodePayload(dataToSend);
            try
            {
                await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
                return true;
            }
            catch
            {
                _logger.Debug($"{GetType().Name} send failed, disconnecting...");
                Disconnect();
            }

            return false;
        }

        public void Disconnect()
        {
            _connectingTokenSource?.Cancel();
            _tcpClient?.Close();
            _tcpReceiver?.StopReceiving();
            IsConnected = false;
            _tcpClient = null;
        }
    }
}
