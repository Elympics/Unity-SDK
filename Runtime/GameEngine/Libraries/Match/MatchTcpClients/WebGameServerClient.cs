using System;
using System.Threading;
using System.Threading.Tasks;
using Elympics;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.WebRtc;
using WebRtcWrapper;

namespace MatchTcpClients
{
    public sealed class WebGameServerClient : GameServerClient
    {
        private readonly IGameServerWebSignalingClient _signalingClient;
        private readonly Func<IWebRtcClient> _webRtcFactory;

        private IWebRtcClient _webRtcClient;
        private string _answer;

        public WebGameServerClient(
            IGameServerSerializer serializer,
            GameServerClientConfig config,
            IGameServerWebSignalingClient signalingClient,
            Func<IWebRtcClient> customWebRtcFactory = null) : base(serializer, config)
        {
            _signalingClient = signalingClient;
            _webRtcFactory = customWebRtcFactory ?? (() => new WebRtcClient());
        }

        public static Uri GetSignalingEndpoint(string gsEndpoint, string publicWebEndpoint, string matchId, string regionName)
        {
            var signalingEndpoint = Uri.TryCreate(publicWebEndpoint, UriKind.Absolute, out var baseUri)
                ? new Uri(baseUri, $"doSignaling/{matchId}")
                : new Uri(new Uri(gsEndpoint), $"{publicWebEndpoint}/doSignaling/{matchId}");

            if (string.IsNullOrEmpty(regionName))
                return signalingEndpoint;

            var uriBuilder = new UriBuilder(signalingEndpoint);
            uriBuilder.Host = regionName + "-" + uriBuilder.Host;
            return uriBuilder.Uri;
        }

        protected override void CreateNetworkClients()
        {
            _webRtcClient = _webRtcFactory();
            ReliableClient = new WebRtcReliableNetworkClient(_webRtcClient);
            UnreliableClient = new WebRtcUnreliableNetworkClient(_webRtcClient);
        }

        protected override async Task<bool> ConnectInternalAsync(CancellationToken ct = default)
        {
            _webRtcClient.ReceiveWithThread();
            _answer = null;
            var (offer, offerSet) = await TryCreateOfferAsync();
            if (!offerSet)
                ElympicsLogger.LogError("Error creating WebRTC offer.");
            if (string.IsNullOrEmpty(offer))
                ElympicsLogger.LogError("Created WebRTC offer is null or empty.");
            if (!offerSet || string.IsNullOrEmpty(offer))
            {
                Disconnect();
                return false;
            }

            var response = await WaitForWebResponseAsync(_signalingClient, offer, ct);
            if (response?.IsError == true || string.IsNullOrEmpty(response?.Text))
            {
                ElympicsLogger.LogError("No valid WebRTC answer has been received.");
                Disconnect();
                return false;
            }
            _answer = response.Text;

            return await TryConnectSessionAsync(ct);
        }

        protected override Task<bool> TryInitializeSessionAsync(CancellationToken ct = default)
        {
            ElympicsLogger.Log("Setting up WebRTC client...");
            _webRtcClient.OnAnswer(_answer);
            ElympicsLogger.Log("Client initialized successfully.");
            return Task.FromResult(true);
        }

        private async Task<WebSignalingClientResponse> WaitForWebResponseAsync(IGameServerWebSignalingClient signalingClient, string offer, CancellationToken ct)
        {
            WebSignalingClientResponse response = null;
            for (var i = 0; i < Config.OfferMaxRetries; i++)
            {
                if (ct.IsCancellationRequested)
                    break;
                var cts = new CancellationTokenSource();

                void OnAnswerReceived(WebSignalingClientResponse r)
                {
                    signalingClient.ReceivedResponse -= OnAnswerReceived;
                    response = r;
                    cts.Cancel();
                }

                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

                ElympicsLogger.Log($"Posting created WebRTC offer.\nAttempt #{i + 1}");
                signalingClient.ReceivedResponse += OnAnswerReceived;
                signalingClient.PostOfferAsync(offer, (int)Math.Ceiling(Config.OfferTimeout.TotalSeconds), linkedCts.Token);

                await TaskUtil.Delay(Config.OfferTimeout, linkedCts.Token).CatchOperationCanceledException();
                signalingClient.ReceivedResponse -= OnAnswerReceived;
                cts.Cancel();

                if (response?.IsError == false)
                    break;
                ElympicsLogger.LogError($"WebRTC answer error: {response?.Text}");

                await TaskUtil.Delay(Config.OfferRetryDelay, linkedCts.Token).CatchOperationCanceledException();
            }

            return response;
        }

        private async Task<(string offer, bool offerSet)> TryCreateOfferAsync()
        {
            string offer = null;
            var offerSet = false;
            var cts = new CancellationTokenSource();

            void OnOfferCreated(string s)
            {
                _webRtcClient.OfferCreated -= OnOfferCreated;
                offer = s;
                offerSet = true;
                cts.Cancel();
            }

            _webRtcClient.OfferCreated += OnOfferCreated;
            _webRtcClient.CreateOffer();
            await TaskUtil.Delay(Config.OfferTimeout, cts.Token).CatchOperationCanceledException();
            _webRtcClient.OfferCreated -= OnOfferCreated;
            return (offer, offerSet);
        }

        protected override void InitializeNetworkClients()
        {
            InitWebRtcClient();
            base.InitializeNetworkClients();
        }

        private void InitWebRtcClient()
        {
            _ = ClientDisconnectedCts.Token.Register(_webRtcClient.Dispose);
        }
    }
}
