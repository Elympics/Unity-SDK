using System;
using System.Net;
using System.Threading.Tasks;
using MatchTcpLibrary.TransportLayer.Interfaces;

#pragma warning disable CS0067

namespace MatchTcpLibrary.TransportLayer.Udp
{
    public class UdpNetworkServerClient : IUnreliableNetworkServerClient
    {
        public event Action Disconnected;
        public event Action<byte[]> DataReceived;
        public event Action<byte[], IPEndPoint> DataReceivedWithSource;

        private readonly IUnreliableNetworkClient _unreliableNetworkClient;
        private IPEndPoint _destination;

        public IPEndPoint LocalEndPoint => _unreliableNetworkClient.LocalEndPoint;

        public IPEndPoint RemoteEndpoint => throw new NotSupportedException();

        public bool IsConnected => _destination != null;

        public UdpNetworkServerClient(IUnreliableNetworkClient unreliableNetworkClient) =>
            _unreliableNetworkClient = unreliableNetworkClient;

        public void CreateAndBind() => throw new NotSupportedException();
        public void CreateAndBind(int port) => throw new NotSupportedException();
        public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotSupportedException();
        public Task<bool> ConnectAsync(IPEndPoint remoteEndPoint) => throw new NotSupportedException();

        public async Task<bool> SendAsync(byte[] payload) =>
            _destination != null && await _unreliableNetworkClient.SendToAsync(payload, _destination);

        public void Disconnect() =>
            _destination = null;

        public Task<bool> SendToAsync(byte[] payload, IPEndPoint destination) => throw new NotSupportedException();
        public void UpdateDestination(IPEndPoint destination) => _destination = destination;

        public void OnDataReceived(byte[] data)
        {
            if (_destination == null)
                return;
            DataReceived?.Invoke(data);
        }
    }
}
