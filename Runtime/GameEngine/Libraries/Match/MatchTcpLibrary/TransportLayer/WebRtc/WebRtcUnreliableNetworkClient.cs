using System;
using System.Net;
using System.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;
using WebRtcWrapper;

#pragma warning disable CS0067

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    public class WebRtcUnreliableNetworkClient : IUnreliableNetworkClient
    {
        private readonly IWebRtcClient _webRtcClient;

        public bool IsConnected { get; private set; }

        public event Action Disconnected;
        public event Action<byte[]> DataReceived;

        public IPEndPoint LocalEndPoint => throw new NotImplementedException();

        public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

        public WebRtcUnreliableNetworkClient(IWebRtcClient webRtcClient)
        {
            _webRtcClient = webRtcClient;
        }

        public void CreateAndBind()
        {
            IsConnected = true;
            _webRtcClient.UnreliableReceivingEnded += () =>
            {
                ElympicsLogger.Log($"{nameof(WebRtcUnreliableNetworkClient)} receiving ended");
                IsConnected = false;
                Disconnected?.Invoke();
            };
            _webRtcClient.UnreliableReceived += data => DataReceived?.Invoke(data);
        }

        public void CreateAndBind(int port) => throw new NotImplementedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();
        public Task<bool> ConnectAsync(IPEndPoint remoteEndPoint) => throw new NotImplementedException();

        public Task<bool> SendAsync(byte[] payload)
        {
            if (IsConnected)
                _webRtcClient.SendUnreliable(payload);
            return Task.FromResult(IsConnected);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public event Action<byte[], IPEndPoint> DataReceivedWithSource;

        public Task<bool> SendToAsync(byte[] payload, IPEndPoint destination) => throw new NotImplementedException();
    }
}
