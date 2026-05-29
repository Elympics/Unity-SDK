using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.Udp
{
    public class UdpNetworkClient : IUnreliableNetworkClient
    {
        public event Action Disconnected;
        public event Action<byte[]> DataReceived;
        public event Action<byte[], IPEndPoint> DataReceivedWithSource;

        private readonly IPEndPoint _anyEndPoint = new(IPAddress.Any, 0);

        public IPEndPoint LocalEndPoint => _udpClient?.Client?.IsBound ?? false
            ? _udpClient?.Client?.LocalEndPoint as IPEndPoint
            : null;

        public IPEndPoint RemoteEndpoint => _udpClient?.Client?.Connected ?? false
            ? _udpClient?.Client?.RemoteEndPoint as IPEndPoint
            : null;

        private UdpClient _udpClient;
        private UdpReceiver _udpReceiver;
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
                    if (_isConnected != value && !value)
                        isDisconnected = true;
                    _isConnected = value;
                }

                if (isDisconnected)
                    Disconnected?.Invoke();
            }
        }

        public void CreateAndBind() => CreateAndBind(_anyEndPoint);

        public void CreateAndBind(int port) => CreateAndBind(new IPEndPoint(IPAddress.Any, port));

        public void CreateAndBind(IPEndPoint localEndPoint)
        {
            Disconnect();
            _udpClient = new UdpClient();
            _udpClient.Client.Bind(localEndPoint);
            SetupUnderlyingUdpClient();
        }

        private void SetupUnderlyingUdpClient()
        {
            _previousLocalEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint;

            _udpReceiver = new UdpReceiver(_udpClient);
            _udpReceiver.DataReceived += OnDataReceived;
            _udpReceiver.StartReceiving();

            IsConnected = _udpClient?.Client.Connected ?? false;
        }

        public UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default)
        {
            try
            {
                if (CheckIfConnectingAndSet())
                    throw ElympicsLogger.LogException(new InvalidOperationException("Connection already in progress"));

                if (NotCreated())
                    throw ElympicsLogger.LogException(new InvalidOperationException($"{nameof(CreateAndBind)} has not been called before connecting"));
                else if (IsDisconnected() || IsConnectedToOther(remoteEndPoint))
                    RecreateSocket();
                else if (IsConnectedTo(remoteEndPoint))
                    return UniTask.CompletedTask;

                _udpClient.Connect(remoteEndPoint);
                IsConnected = true;

                return UniTask.CompletedTask;
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

        private bool NotCreated() => _udpClient == null && _previousLocalEndPoint == null;

        private bool IsDisconnected() => _udpClient == null && _previousLocalEndPoint != null;

        private bool IsConnectedTo(IPEndPoint remoteEndPoint) => IsConnected && remoteEndPoint.Equals(RemoteEndpoint);

        private bool IsConnectedToOther(IPEndPoint remoteEndPoint) => IsConnected && !remoteEndPoint.Equals(RemoteEndpoint);

        private void RecreateSocket() => CreateAndBind(_previousLocalEndPoint);

        private void OnDataReceived(byte[] data, IPEndPoint sourceEndPoint, DateTime _)
        {
            DataReceived?.Invoke(data);
            DataReceivedWithSource?.Invoke(data, sourceEndPoint);
        }

        public async UniTask SendAsync(byte[] payload)
        {
            if (!IsConnected)
                throw ElympicsLogger.LogException(new InvalidOperationException("Not connected"));

            try
            {
                _ = await _udpClient.SendAsync(payload, payload.Length).AsUniTask();
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException("Error while sending data through the UDP socket", e);
                throw;
            }
        }

        public async UniTask SendToAsync(byte[] payload, IPEndPoint destination)
        {
            if (IsConnected)
                throw ElympicsLogger.LogException(new InvalidOperationException("Not connected"));

            try
            {
                _ = await _udpClient.Client.SendToAsync(new ArraySegment<byte>(payload, 0, payload.Length), SocketFlags.None, destination).AsUniTask();
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException("Error while sending data through the UDP socket", e);
                throw;
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            _udpClient?.Close();
            // If UdpClient is closed UdpReceived should close either
            _udpClient = null;
        }
        public void Dispose()
        { }
    }
}
