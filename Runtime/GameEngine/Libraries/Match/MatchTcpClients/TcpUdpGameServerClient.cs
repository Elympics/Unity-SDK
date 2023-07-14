using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpLibrary.TransportLayer.SimpleMessageEncoder;
using MatchTcpLibrary.TransportLayer.Tcp;
using MatchTcpLibrary.TransportLayer.Udp;

namespace MatchTcpClients
{
    public sealed class TcpUdpGameServerClient : GameServerClient
    {
        private readonly IPEndPoint _endpoint;

        public TcpUdpGameServerClient(
            IGameServerClientLogger logger,
            IGameServerSerializer serializer,
            GameServerClientConfig config,
            IPEndPoint endpoint) : base(logger, serializer, config)
        {
            _endpoint = endpoint;
        }

        protected override void CreateNetworkClients()
        {
            ReliableClient = CreateTcpNetworkClient();
            UnreliableClient = CreateUdpNetworkClient();
        }

        protected override async Task<bool> ConnectInternalAsync(CancellationToken ct = default)
        {
            Logger.Info($"[Elympics] Connecting reliable to {_endpoint}");
            if (!await TryConnectSessionAsync(ct))
                return false;

            Logger.Info($"[Elympics] Connecting unreliable to {_endpoint}");
            if (await UnreliableClient.ConnectAsync(_endpoint))
                return true;

            Disconnect();
            return false;
        }

        protected override async Task<bool> TryInitializeSessionAsync(CancellationToken ct = default)
        {
            try
            {
                return await ReliableClient.ConnectAsync(_endpoint);
            }
            catch (SocketException e)
            {
                Logger.Error($"Couldn't connect to the server: {e}");
                return false;
            }
        }

        private IReliableNetworkClient CreateTcpNetworkClient()
        {
            var encoder = new SimpleDelimiterEncoder(SimpleMessageEncoderConfig.Default);
            var client = new TcpNetworkClient(MatchTcpLibraryLogger, encoder, TcpProtocolConfig.Default);
            return client;
        }

        private IUnreliableNetworkClient CreateUdpNetworkClient()
        {
            return new UdpNetworkClient(MatchTcpLibraryLogger);
        }
    }
}
