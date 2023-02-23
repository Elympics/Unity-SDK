using System;
using System.Threading;
using System.Threading.Tasks;
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
			IGameServerClientLogger logger,
			IGameServerSerializer serializer,
			GameServerClientConfig config,
			IGameServerWebSignalingClient signalingClient,
			Func<IWebRtcClient> customWebRtcFactory = null) : base(logger, serializer, config)
		{
			_signalingClient = signalingClient;
			_webRtcFactory = customWebRtcFactory ?? (() => new WebRtcClient());
		}

		public static Uri GetSignalingEndpoint(string gsEndpoint, string publicWebEndpoint, string matchId, string regionName = null)
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
			ReliableClient = new WebRtcReliableNetworkClient(MatchTcpLibraryLogger, _webRtcClient);
			UnreliableClient = new WebRtcUnreliableNetworkClient(MatchTcpLibraryLogger, _webRtcClient);
		}

		protected override async Task<bool> ConnectInternalAsync(CancellationToken ct = default)
		{
			_answer = null;
			var (offer, offerSet) = await TryCreateOfferAsync();
			if (!offerSet)
				Logger.Error("[Elympics] WebRTC Offer not received from WebRTC client");
			if (string.IsNullOrEmpty(offer))
				Logger.Error("[Elympics] WebRTC Offer is null or empty");
			if (!offerSet || string.IsNullOrEmpty(offer))
			{
				Disconnect();
				return false;
			}

			var response = await WaitForWebResponseAsync(_signalingClient, offer, ct);
			if (response?.IsError == true || string.IsNullOrEmpty(response?.Text))
			{
				Logger.Error("[Elympics] WebRTC Answer is null or empty");
				Disconnect();
				return false;
			}
			_answer = response.Text;

			return await TryConnectSessionAsync(ct);
		}

		protected override Task<bool> TryInitializeSessionAsync(CancellationToken ct = default)
		{
			Logger.Info("[Elympics] Setting up WebRTC client");
			_webRtcClient.OnAnswer(_answer);
			return Task.FromResult(true);
		}

		private async Task<WebSignalingResponse> WaitForWebResponseAsync(IGameServerWebSignalingClient signalingClient, string offer, CancellationToken ct)
		{
			WebSignalingResponse response = null;
			for (var i = 0; i < Config.OfferMaxRetries; i++)
			{
				if (ct.IsCancellationRequested)
					break;
				var cts = new CancellationTokenSource();

				void OnAnswerReceived(WebSignalingResponse r)
				{
					signalingClient.ReceivedResponse -= OnAnswerReceived;
					response = r;
					cts.Cancel();
				}

				var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

				signalingClient.ReceivedResponse += OnAnswerReceived;
				signalingClient.PostOfferAsync(offer, (int)Math.Ceiling(Config.OfferTimeout.TotalSeconds), linkedCts.Token);

				await Task.Delay(Config.OfferTimeout, linkedCts.Token).CatchOperationCanceledException();
				signalingClient.ReceivedResponse -= OnAnswerReceived;
				cts.Cancel();

				if (response?.IsError == false)
					break;
				Logger.Error("[Elympics] WebRTC answer error: {0}", response?.Text);

				await Task.Delay(Config.OfferRetryDelay, linkedCts.Token).CatchOperationCanceledException();
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
			await Task.Delay(Config.OfferTimeout, cts.Token).CatchOperationCanceledException();
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
			ClientDisconnectedCts.Token.Register(_webRtcClient.Dispose);
		}
	}
}
