using System;
using System.Net;
using System.Threading.Tasks;
using MatchTcpLibrary.TransportLayer.Interfaces;
using WebRtcWrapper;

#pragma warning disable CS0067

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    public class WebRtcUnreliableNetworkServerClient : IUnreliableNetworkServerClient
    {
        private readonly IMatchTcpLibraryLogger _logger;
        private readonly IWebRtcServerClient _webRtcServerClient;

        public bool IsConnected { get; private set; }

        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        public IPEndPoint LocalEndPoint => throw new NotImplementedException();

        public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

        public WebRtcUnreliableNetworkServerClient(IMatchTcpLibraryLogger logger, IWebRtcServerClient webRtcServerClient)
        {
            _logger = logger;
            _webRtcServerClient = webRtcServerClient;
        }

        public void CreateAndBind()
        {
            IsConnected = true;
            _webRtcServerClient.UnreliableReceivingEnded += () =>
            {
                _logger.Info($"{nameof(WebRtcUnreliableNetworkServerClient)} receiving ended");
                IsConnected = false;
                Disconnected?.Invoke();
            };
            _webRtcServerClient.UnreliableReceived += data => DataReceived?.Invoke(data);
        }

        public void CreateAndBind(int port) => throw new NotImplementedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();
        public Task<bool> ConnectAsync(IPEndPoint remoteEndPoint) => throw new NotImplementedException();

        public Task<bool> SendAsync(byte[] payload)
        {
            if (IsConnected)
                _webRtcServerClient.SendUnreliable(payload);
            return Task.FromResult(IsConnected);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public event Action<byte[], IPEndPoint> DataReceivedWithSource;

        public Task<bool> SendToAsync(byte[] payload, IPEndPoint destination) => throw new NotImplementedException();

        public void UpdateDestination(IPEndPoint destination) => throw new NotImplementedException();

        public void OnDataReceived(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
