using System;
using System.Net;
using System.Threading.Tasks;
using MatchTcpLibrary.TransportLayer.Interfaces;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    public class WebRtcReliableNetworkServerClient : IReliableNetworkClient
    {
        private readonly IMatchTcpLibraryLogger _logger;
        private readonly IWebRtcServerClient _webRtcServerClient;

        public bool IsConnected { get; private set; }

        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        public IPEndPoint LocalEndPoint => throw new NotImplementedException();

        public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

        public WebRtcReliableNetworkServerClient(IMatchTcpLibraryLogger logger, IWebRtcServerClient webRtcServerClient)
        {
            _logger = logger;
            _webRtcServerClient = webRtcServerClient;
        }

        public void CreateAndBind()
        {
            IsConnected = true;
            _webRtcServerClient.ReliableReceivingEnded += () =>
            {
                _logger.Info($"{nameof(WebRtcReliableNetworkClient)} receiving ended");
                IsConnected = false;
                Disconnected?.Invoke();
            };
            _webRtcServerClient.ReliableReceived += data => DataReceived?.Invoke(data);
        }

        public void CreateAndBind(int port) => throw new NotImplementedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();
        public Task<bool> ConnectAsync(IPEndPoint remoteEndPoint) => throw new NotImplementedException();

        public Task<bool> SendAsync(byte[] payload)
        {
            if (IsConnected)
                _webRtcServerClient.SendReliable(payload);
            return Task.FromResult(IsConnected);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }
    }
}
