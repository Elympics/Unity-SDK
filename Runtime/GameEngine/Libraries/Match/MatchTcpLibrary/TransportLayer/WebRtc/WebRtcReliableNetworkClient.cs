using System;
using System.Net;
using System.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    public class WebRtcReliableNetworkClient : IReliableNetworkClient
    {
        private readonly IWebRtcClient _webRtcClient;

        public bool IsConnected { get; private set; }

        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        public IPEndPoint LocalEndPoint => throw new NotImplementedException();

        public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

        public WebRtcReliableNetworkClient(IWebRtcClient webRtcClient)
        {
            _webRtcClient = webRtcClient;
        }

        public void CreateAndBind()
        {
            IsConnected = true;
            _webRtcClient.ReliableReceivingEnded += () =>
            {
                ElympicsLogger.Log($"{nameof(WebRtcReliableNetworkClient)} receiving ended");
                IsConnected = false;
                Disconnected?.Invoke();
            };
            _webRtcClient.ReliableReceived += data => DataReceived?.Invoke(data);
        }

        public void CreateAndBind(int port) => throw new NotImplementedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();
        public Task<bool> ConnectAsync(IPEndPoint remoteEndPoint) => throw new NotImplementedException();

        public Task<bool> SendAsync(byte[] payload)
        {
            if (IsConnected)
                _webRtcClient.SendReliable(payload);
            return Task.FromResult(IsConnected);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }
    }
}
