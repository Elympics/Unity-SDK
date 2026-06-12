#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.GameEngine.Libraries.WebRtc;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using MatchTcpLibrary.TransportLayer.WebRtc;

namespace MatchTcpClients
{
    internal sealed class WebGameServerClient : GameServerClient
    {
        private readonly IGameServerWebSignalingClient _signalingClient;
        private const string RouteVersion = "v2";

        public WebGameServerClient(
            IGameServerSerializer serializer,
            GameServerClientConfig config,
            IGameServerWebSignalingClient signalingClient) : base(serializer, config) =>
            _signalingClient = signalingClient;

        public static Uri GetSignalingServerBaseAddress(string gsEndpoint, string publicWebEndpoint, string? regionName)
        {
            var baseAddress = Uri.TryCreate(publicWebEndpoint, UriKind.Absolute, out var baseUri)
                ? new Uri(baseUri, $"{RouteVersion}/")
                : new Uri(new Uri(gsEndpoint), $"{publicWebEndpoint}/{RouteVersion}/");

            if (string.IsNullOrEmpty(regionName))
                return baseAddress;

            var uriBuilder = new UriBuilder(baseAddress);
            uriBuilder.Host = regionName + "-" + uriBuilder.Host;
            return uriBuilder.Uri;
        }

        protected override INetworkClient CreateNetworkClient()
        {
            var webRtcConfig = WebRtcConfig.Default;
            webRtcConfig.OfferAnnounceDelay = Config.OfferAnnounceDelay;
            return new WebRtcNetworkClient(_signalingClient, webRtcConfig, Config,
                new[]
                {
                    (INetworkClient.ReliableLabel, true),
                    (INetworkClient.UnreliableLabel, false),
                });
        }

        protected override UniTask ConnectInternalAsync(CancellationToken ct = default) => NetworkClient!.Connect(ct);
    }
}
