using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpLibrary.TransportLayer.SimpleMessageEncoder;
using MatchTcpLibrary.TransportLayer.Tcp;
using MatchTcpLibrary.TransportLayer.Udp;

namespace MatchTcpClients
{
    internal sealed class TcpUdpGameServerClient : GameServerClient
    {
        private readonly IPEndPoint _endpoint;

        public TcpUdpGameServerClient(
            IGameServerSerializer serializer,
            GameServerClientConfig config,
            IPEndPoint endpoint) : base(serializer, config) =>
            _endpoint = endpoint;

        protected override void CreateNetworkClients()
        {
            ReliableClient?.Dispose();
            ReliableClient = CreateTcpNetworkClient();
            UnreliableClient?.Dispose();
            UnreliableClient = CreateUdpNetworkClient();
        }

        protected override async UniTask ConnectInternalAsync(CancellationToken ct = default)
        {
            try
            {
                ElympicsLogger.Log($"Connecting reliable to {_endpoint}");
                await ConnectSessionAsync(ct);

                ElympicsLogger.Log($"Connecting unreliable to {_endpoint}");
                await UnreliableClient.ConnectAsync(_endpoint, ct);
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        protected override async UniTask InitializeSessionAsync(CancellationToken ct = default)
        {
            try
            {
                await ReliableClient.ConnectAsync(_endpoint, ct);
            }
            catch (SocketException e)
            {
                _ = ElympicsLogger.LogException("Couldn't connect to the server", e);
                throw;
            }
        }

        private IReliableNetworkClient CreateTcpNetworkClient()
        {
            var encoder = new SimpleDelimiterEncoder(SimpleMessageEncoderConfig.Default);
            var client = new TcpNetworkClient(encoder, TcpProtocolConfig.Default);
            return client;
        }

        private IUnreliableNetworkClient CreateUdpNetworkClient() =>
            new UdpNetworkClient();
    }
}
