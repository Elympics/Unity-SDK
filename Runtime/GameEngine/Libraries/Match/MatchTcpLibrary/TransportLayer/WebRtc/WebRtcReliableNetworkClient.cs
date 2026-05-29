using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    internal class WebRtcReliableNetworkClient : IReliableNetworkClient
    {
        private readonly IWebRtcClient _webRtcClient;

        public bool IsConnected { get; private set; }

        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        public IPEndPoint LocalEndPoint => throw new NotImplementedException();

        public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

        public WebRtcReliableNetworkClient(IWebRtcClient webRtcClient) => _webRtcClient = webRtcClient;

        public void CreateAndBind()
        {
            IsConnected = true;
            _webRtcClient.ReliableReceivingEnded += OnWebRtcClientOnReliableReceivingEnded;
            _webRtcClient.ReliableReceived += OnWebRtcClientOnReliableReceived;
        }
        private void OnWebRtcClientOnReliableReceived(byte[] data) => DataReceived?.Invoke(data);
        private void OnWebRtcClientOnReliableReceivingEnded()
        {
            ElympicsLogger.Log($"{nameof(WebRtcReliableNetworkClient)} receiving ended");
            IsConnected = false;
            Disconnected?.Invoke();
        }

        public void CreateAndBind(int port) => throw new NotImplementedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();
        public UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default) => throw new NotImplementedException();

        public UniTask SendAsync(byte[] payload)
        {
            if (!IsConnected)
                throw ElympicsLogger.LogException(new InvalidOperationException("Not connected"));
            _webRtcClient.SendReliable(payload);
            return UniTask.CompletedTask;
        }

        public void Disconnect() => IsConnected = false;

        public void Dispose()
        {
            _webRtcClient.ReliableReceivingEnded -= OnWebRtcClientOnReliableReceivingEnded;
            _webRtcClient.ReliableReceived -= OnWebRtcClientOnReliableReceived;
        }
    }
}
