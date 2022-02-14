using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Elympics.Libraries;
using MatchTcpClients.Synchronizer;
using Proto.ProtoClient.NetworkClient;
using UnityConnectors.HalfRemote;
using UnityEngine;
using WebRtcWrapper;
using Debug = UnityEngine.Debug;

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
		private readonly SimpleHttpSignalingClient    _signalingClient;

		private TcpClient     _tcpClient;
		private IWebRtcClient _webRtcClient;

		public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, string ip, int port, string userId, bool useWeb)
		{
			_halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
			_ip = ip;
			_port = port;
			_userId = userId;
			_useWeb = useWeb;
			if (useWeb)
				_signalingClient = new SimpleHttpSignalingClient(new Uri($"http://{_ip}:{_port}"));
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

				yield return _signalingClient.PostOfferAsync(offer);
				if (_signalingClient.Request.isNetworkError)
				{
					Debug.LogError(_signalingClient.Request.error);
				}
				else if (_signalingClient.Request.isHttpError)
				{
					Debug.Log(_signalingClient.Request.downloadHandler.text);
				}
				else
				{
					answer = _signalingClient.Request.downloadHandler.text;
					break;
				}

				yield return WaitTimeToRetryConnect;
			}

			if (string.IsNullOrEmpty(answer))
			{
				Debug.LogError("WebRTC Answer empty - connection error or signaling server problem");
				connectedCallback.Invoke(false);
				yield break;
			}
			
			_webRtcClient.OnAnswer(answer);

			var client = new HalfRemoteMatchClient(_userId, _webRtcClient);
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
