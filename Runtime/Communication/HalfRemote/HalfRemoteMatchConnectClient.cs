using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Elympics.Libraries;
using MatchTcpClients.Synchronizer;
using MatchTcpLibrary;
using Proto.ProtoClient.NetworkClient;
using UnityConnectors.HalfRemote;
using UnityEngine;
using WebRtcWrapper;
using Debug = UnityEngine.Debug;

#pragma warning disable CS0067

namespace Elympics
{
	public class HalfRemoteMatchConnectClient : IMatchConnectClient
	{
		private const           int            ConnectMaxRetries      = 50;
		private static readonly WaitForSeconds WaitTimeToRetryConnect = new WaitForSeconds(1);

		private static readonly WaitForSeconds OfferWaitingInterval     = new WaitForSeconds(1);
		private const           int            MaxOfferWaitingIntervals = 5;

		public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		public event Action                          ConnectingFailed;
		public event Action<string>                  AuthenticatedUserMatchWithUserId;
		public event Action<string>                  AuthenticatedUserMatchFailedWithError;
		public event Action                          AuthenticatedAsSpectator;
		public event Action<string>                  AuthenticatedAsSpectatorWithError;
		public event Action<string>                  MatchJoinedWithError;
		public event Action<string>                  MatchJoinedWithMatchId;
		public event Action<string>                  MatchEndedWithMatchId;
		public event Action                          DisconnectedByServer;
		public event Action                          DisconnectedByClient;

		private static readonly string MatchId = Guid.NewGuid().ToString();

		private readonly HalfRemoteMatchClientAdapter _halfRemoteMatchClientAdapter;
		private readonly string                       _ip;
		private readonly int                          _port;
		private readonly string                       _userId;
		private readonly bool                         _useWeb;
		private readonly HttpSignalingClient          _signalingClient;

		private TcpClient     _tcpClient;
		private IWebRtcClient _webRtcClient;

		private IGameServerWebSignalingClient.Response _webResponse;

		public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, string ip, int port, string userId, bool useWeb)
		{
			_halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
			_ip = ip;
			_port = port;
			_userId = userId;
			_useWeb = useWeb;
			if (useWeb)
			{
				var baseUri = new Uri($"http://{_ip}:{_port}");
				_signalingClient = new HttpSignalingClient(new Uri(baseUri, "/doSignaling"));
			}
		}

		public IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)
		{
			return _useWeb
				? ConnectUsingWeb(ConnectedCallback, ct)
				: ConnectUsingTcp(ConnectedCallback, ct);

			void ConnectedCallback(bool connected)
			{
				if (!connected)
					return;

				_halfRemoteMatchClientAdapter.PlayerConnected();
				ConnectedWithSynchronizationData?.Invoke(new TimeSynchronizationData {LocalClockOffset = TimeSpan.Zero, RoundTripDelay = TimeSpan.Zero, UnreliableReceivedAnyPing = false, UnreliableWaitingForFirstPing = true});
				AuthenticatedUserMatchWithUserId?.Invoke(_userId);
				MatchJoinedWithMatchId?.Invoke(MatchId);
			}
		}

		private IEnumerator ConnectUsingTcp(Action<bool> connectedCallback, CancellationToken ct)
		{
			for (var i = 0; i < ConnectMaxRetries; i++)
			{
				if (!Application.isPlaying)
					yield break;
				try
				{
					_tcpClient = new TcpClient();
					_tcpClient.Connect(IPAddress.Parse(_ip), _port);
					Debug.Log($"Tcp connected to {_ip}:{_port}");
					break;
				}
				catch (Exception e)
				{
					_tcpClient = null;
					Debug.LogException(e);
				}

				yield return WaitTimeToRetryConnect;
			}

			if (_tcpClient == null)
			{
				connectedCallback.Invoke(false);
				yield break;
			}

			var client = new HalfRemoteMatchClient(_userId, new ProtoNetworkStreamClient(_tcpClient.GetStream()));
			yield return _halfRemoteMatchClientAdapter.ConnectToServer(connectedCallback, _userId, client);
		}

		private IEnumerator ConnectUsingWeb(Action<bool> connectedCallback, CancellationToken ct)
		{
			_webRtcClient = WebRtcFactory.CreateInstance();
			string offer = null;
			var offerSet = false;
			_webRtcClient.OfferCreated += s =>
			{
				offer = s;
				offerSet = true;
			};
			_webRtcClient.CreateOffer();

			for (var i = 0; i < MaxOfferWaitingIntervals; i++)
			{
				if (offerSet)
					break;
				yield return OfferWaitingInterval;
			}

			if (!offerSet)
				throw new ArgumentException("Offer not received from WebRTC client");
			if (string.IsNullOrEmpty(offer))
				throw new ArgumentException("Offer is null or empty");

			string answer = null;
			for (var i = 0; i < ConnectMaxRetries; i++)
			{
				if (!Application.isPlaying)
					yield break;

				_webResponse = null;
				_signalingClient.ReceivedResponse += r => _webResponse = r;
				_signalingClient.PostOfferAsync(offer, 1, ct);
				yield return WaitTimeToRetryConnect;

				if (_webResponse?.IsError == false)
				{
					answer = _webResponse.Text;
					break;
				}
				Debug.LogError(_webResponse?.IsError == true ? _webResponse.Text : "Response not received from WebRTC client");
			}

			if (string.IsNullOrEmpty(answer))
			{
				Debug.LogError("WebRTC Answer empty - connection error or signaling server problem");
				connectedCallback.Invoke(false);
				yield break;
			}


			var channelOpened = false;
			var client = new HalfRemoteMatchClient(_userId, _webRtcClient);

			void ChannelOpenedHandler(byte[] data, string playerId)
			{
				channelOpened = true;
			}

			client.InGameDataForPlayerOnUnreliableChannelGenerated += ChannelOpenedHandler;

			_webRtcClient.OnAnswer(answer);

			for (var i = 0; i < ConnectMaxRetries; i++)
			{
				if (!Application.isPlaying)
					yield break;

				if (channelOpened)
					break;
				yield return WaitTimeToRetryConnect;
			}

			client.InGameDataForPlayerOnUnreliableChannelGenerated -= ChannelOpenedHandler;

			if (!channelOpened)
			{
				Debug.LogError("WebRTC channel not opened after time");
				connectedCallback.Invoke(false);
				yield break;
			}

			Debug.Log("WebRTC received channel opened");

			yield return _halfRemoteMatchClientAdapter.ConnectToServer(connectedCallback, _userId, client);
		}

		public IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct)
		{
			connectedCallback?.Invoke(false);
			yield break;
		}

		public void Disconnect()
		{
			_halfRemoteMatchClientAdapter.PlayerDisconnected();
			_tcpClient?.Dispose();
			_webRtcClient?.Dispose();
			DisconnectedByClient?.Invoke();
		}

		public void Dispose() => Disconnect();
	}
}
