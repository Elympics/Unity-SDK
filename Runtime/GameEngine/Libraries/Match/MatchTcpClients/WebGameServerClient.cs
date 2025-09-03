using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Communication.Models;
using Elympics.ElympicsSystems.Internal;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.WebRtc;
using UnityEngine;
using WebRtcWrapper;

#nullable enable

namespace MatchTcpClients
{
    internal sealed class WebGameServerClient : GameServerClient
    {
        private readonly IGameServerWebSignalingClient _signalingClient;
        private readonly Func<TimeSpan, IWebRtcClient> _webRtcFactory;

        private IWebRtcClient? _webRtcClient;
        private string? _answer;
        private readonly ElympicsLoggerContext _logger;
        private CancellationTokenSource? _stateCancellationTokenSource;
        private CancellationTokenSource? _linkedCts;
        private readonly List<string> _candidates = new();
        private const string RouteVersion = "v2";

        public WebGameServerClient(
            IGameServerSerializer serializer,
            GameServerClientConfig config,
            IGameServerWebSignalingClient signalingClient,
            ElympicsLoggerContext logger,
            Func<TimeSpan, IWebRtcClient>? customWebRtcFactory = null) : base(serializer, config, logger)
        {
            _signalingClient = signalingClient;
            _webRtcFactory = customWebRtcFactory ?? (_ => new WebRtcClient());
            _logger = logger.WithContext(nameof(WebGameServerClient));
        }

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

        protected override void CreateNetworkClients()
        {
            _webRtcClient?.Dispose();
            _webRtcClient = _webRtcFactory(Config.OfferAnnounceDelay);
            ReliableClient?.Dispose();
            ReliableClient = new WebRtcReliableNetworkClient(_webRtcClient);
            UnreliableClient?.Dispose();
            UnreliableClient = new WebRtcUnreliableNetworkClient(_webRtcClient);
        }

        protected override async Task<bool> ConnectInternalAsync(CancellationToken ct = default)
        {
            if (_webRtcClient is null)
                throw new InvalidOperationException("WebRTC client not initialized");

            var logger = _logger.WithMethodName();
            _webRtcClient.ReceiveWithThread();
            _answer = null;
            try
            {
                for (var i = 0; i < Config.SessionConnectRetries; i++)
                {
                    if (i > 0)
                        Initialize();

                    _webRtcClient.ConnectionStateChanged += OnConnectionStateChanged;
                    _webRtcClient.IceConnectionStateChanged += OnIceConnectionStateChanged;
                    _webRtcClient.IceCandidateCreated += OnIceCandidateCreated;
                    _stateCancellationTokenSource = new CancellationTokenSource();
                    _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _stateCancellationTokenSource.Token);

                    logger.Log($"Establish the connection attempt #{i + 1}");
                    var (offer, offerSet) = await TryCreateOfferAsync(false);
                    if (!offerSet)
                        logger.Error("Error creating WebRTC offer.");
                    if (string.IsNullOrEmpty(offer))
                        logger.Error("Created WebRTC offer is null or empty.");
                    if (!offerSet || string.IsNullOrEmpty(offer))
                    {
                        Disconnect();
                        return false;
                    }

                    logger.Log($"Send offer:{Environment.NewLine}{offer}");
                    var response = await WaitForWebResponseAsync(_signalingClient, offer, _linkedCts.Token);
                    if (response?.IsError == true || string.IsNullOrEmpty(response?.Text))
                    {
                        logger.Error("No valid WebRTC answer has been received.");
                        Disconnect();
                        return false;
                    }

                    var deserialized = JsonUtility.FromJson<SignalingResponse>(response.Text);

                    _answer = deserialized.answer;
                    logger.Log($"Answer:{Environment.NewLine}{_answer}");
                    var connected = await TryConnectSessionAsync(_linkedCts.Token);

                    _candidates.Clear();
                    _webRtcClient.ConnectionStateChanged -= OnConnectionStateChanged;
                    _webRtcClient.IceConnectionStateChanged -= OnIceConnectionStateChanged;
                    _webRtcClient.IceCandidateCreated -= OnIceCandidateCreated;
                    _stateCancellationTokenSource.Dispose();
                    _linkedCts.Dispose();
                    if (connected)
                        return true;
                    else
                        logger.Warning("Could not establish the connection.");
                }

                logger.Error("Failed to establish WebRtc connection.");
            }
            finally
            {
                _webRtcClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _webRtcClient.IceConnectionStateChanged -= OnIceConnectionStateChanged;
                _stateCancellationTokenSource?.Dispose();
                _linkedCts?.Dispose();
            }

            return false;
        }

        private void OnConnectionStateChanged(string newState)
        {
            if (newState is RtcPeerConnectionStates.Failed)
                _stateCancellationTokenSource?.Cancel();
        }

        private void OnIceConnectionStateChanged(string newState)
        { }

        private void OnIceCandidateCreated(string? newCandidate)
        {
            Debug.Log(newCandidate != null ? $"### New IceCandidate created.{Environment.NewLine}{newCandidate}" : "### No more ICE candidates.");
            if (!string.IsNullOrEmpty(newCandidate))
                _candidates.Add(newCandidate);
        }

        protected override Task<bool> TryInitializeSessionAsync(CancellationToken ct = default)
        {
            if (_webRtcClient is null)
                throw new InvalidOperationException("WebRTC client not initialized");
            if (_answer is null)
                throw new InvalidOperationException("WebRTC answer not set");
            _webRtcClient.OnAnswer(_answer);
            return Task.FromResult(true);
        }

        private async Task<WebSignalingClientResponse?> WaitForWebResponseAsync(IGameServerWebSignalingClient signalingClient, string offer, CancellationToken ct)
        {
            var logger = _logger.WithMethodName();

            for (var i = 0; i < Config.OfferMaxRetries; i++)
            {
                if (ct.IsCancellationRequested)
                    break;

                logger.Log($"Posting created WebRTC offer.\nAttempt #{i + 1}");
                logger.Log($"Sending offer:{Environment.NewLine}{offer}");

                var offerWithCandidates = new OfferWithCandidates
                {
                    offer = offer,
                    candidates = _candidates.ToArray(),
                };

                var result = await signalingClient.PostOfferAsync(JsonUtility.ToJson(offerWithCandidates), TimeSpan.FromSeconds(Config.OfferTimeout.TotalSeconds), ct);
                if (result?.IsError == false)
                    return result;

                logger.Warning($"WebRTC answer error: {result?.Text}");
                await TaskUtil.Delay(Config.OfferRetryDelay, ct).CatchOperationCanceledException();
            }

            return null;
        }

        private async UniTask<(string offer, bool offerSet)> TryCreateOfferAsync(bool restart)
        {
            if (_webRtcClient is null)
                throw new InvalidOperationException("WebRTC client not initialized");

            string? offer = null;
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
            _webRtcClient.CreateOffer(restart);
            _ = await UniTask.Delay(Config.OfferTimeout, DelayType.Realtime, PlayerLoopTiming.Update, cts.Token).SuppressCancellationThrow();
            _webRtcClient.OfferCreated -= OnOfferCreated;
            return (offer!, offerSet);
        }

        protected override void InitializeNetworkClients()
        {
            InitWebRtcClient();
            base.InitializeNetworkClients();
        }

        private void InitWebRtcClient() => _ = ClientDisconnectedCts.Token.Register(() => _webRtcClient?.Dispose());
    }
}
