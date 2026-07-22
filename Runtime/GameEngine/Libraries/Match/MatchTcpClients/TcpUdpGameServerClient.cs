using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpLibrary.TransportLayer.SimpleMessageEncoder;
using MatchTcpLibrary.TransportLayer.Tcp;
using MatchTcpLibrary.TransportLayer.TcpUdp;
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

        protected override INetworkClient CreateNetworkClient() =>
            new TcpUdpNetworkClient(_endpoint, new (string, Func<IDataChannel>)[]
            {
                (INetworkClient.ReliableLabel, CreateTcpNetworkClient),
                (INetworkClient.UnreliableLabel, CreateUdpNetworkClient),
            });

        protected override UniTask ConnectInternalAsync(CancellationToken ct = default) => NetworkClient.Connect(ct);

        private static IDataChannel CreateTcpNetworkClient()
        {
            var encoder = new SimpleDelimiterEncoder(SimpleMessageEncoderConfig.Default);
            var client = new TcpDataChannel(INetworkClient.ReliableLabel, encoder, TcpProtocolConfig.Default);
            return client;
        }

        private static IDataChannel CreateUdpNetworkClient() =>
            new UdpDataChannel(INetworkClient.UnreliableLabel);
    }
}
