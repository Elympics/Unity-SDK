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
            IPEndPoint endpoint) : base(serializer, config)
        {
            _endpoint = endpoint;
        }

        protected override void CreateNetworkClients()
        {
            ReliableClient?.Dispose();
            ReliableClient = CreateTcpNetworkClient();
            UnreliableClient?.Dispose();
            UnreliableClient = CreateUdpNetworkClient();
        }

        protected override async UniTask<bool> ConnectInternalAsync(CancellationToken ct = default)
        {
            ElympicsLogger.Log($"Connecting reliable to {_endpoint}");
            if (!await TryConnectSessionAsync(ct))
            {
                ElympicsLogger.LogError("Could not establish the reliable connection.");
                return false;
            }

            ElympicsLogger.Log($"Connecting unreliable to {_endpoint}");
            if (await UnreliableClient.ConnectAsync(_endpoint))
                return true;

            Disconnect();
            return false;
        }

        protected override async UniTask<bool> TryInitializeSessionAsync(CancellationToken ct = default)
        {
            try
            {
                return await ReliableClient.ConnectAsync(_endpoint);
            }
            catch (SocketException e)
            {
                _ = ElympicsLogger.LogException("Couldn't connect to the server", e);
                return false;
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
