using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HybridWebSocket;
using MatchTcpClients.Synchronizer;
using Proto.ProtoClient.NetworkClient;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Elympics
{
	public class HalfRemoteMatchConnectClient : IMatchConnectClient
	{
		private const           int      ConnectMaxRetries         = 50;
		private static readonly TimeSpan WaitTimeToRetryConnect    = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan WebSocketHandshakeTimeout = TimeSpan.FromSeconds(5);

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
		private readonly bool                         _useWebSockets;
		private readonly bool                         _useWebRtc;

		private TcpClient _tcpClient;
		private WebSocket _websocket;

		public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, string ip, int port, string userId, bool useWebSockets, bool useWebRtc)
		{
			_halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
			_ip = ip;
			_port = port;
			_userId = userId;
			_useWebSockets = useWebSockets;
			_useWebRtc = useWebRtc;
		}

		public IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)
		{
			if (_useWebSockets)
				return ConnectUsingWebSocket(ConnectedCallback, ct);
			return ConnectUsingTcpClient(ConnectedCallback, ct);

			void ConnectedCallback(bool connected)
			{
				if (!connected)
					return;

				_halfRemoteMatchClientAdapter.PlayerConnected();
				if (_useWebRtc)
					_halfRemoteMatchClientAdapter.UpgradeWebRtc();
				ConnectedWithSynchronizationData?.Invoke(new TimeSynchronizationData {LocalClockOffset = TimeSpan.Zero, RoundTripDelay = TimeSpan.Zero, UnreliableReceivedAnyPing = false, UnreliableWaitingForFirstPing = true});
				AuthenticatedUserMatchWithUserId?.Invoke(_userId);
				MatchJoinedWithMatchId?.Invoke(MatchId);
			}
		}

		private IEnumerator ConnectUsingTcpClient(Action<bool> connectedCallback, CancellationToken ct)
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

				yield return new WaitForSeconds((float) WaitTimeToRetryConnect.TotalSeconds);
			}

			if (_tcpClient == null)
			{
				connectedCallback.Invoke(false);
				yield break;
			}

			_halfRemoteMatchClientAdapter.ConnectToServer(new ProtoNetworkStreamClient(_tcpClient.GetStream()), _userId);
			connectedCallback?.Invoke(true);
		}

		private IEnumerator ConnectUsingWebSocket(Action<bool> connectedCallback, CancellationToken ct)
		{
			var url = $"ws://{_ip}:{_port}";
			for (var i = 0; i < ConnectMaxRetries; i++)
			{
				if (!Application.isPlaying)
					yield break;
				bool? connected = null;
				_websocket = WebSocketFactory.CreateInstance(url);
				_websocket.OnOpen += () =>
				{
					Debug.Log($"Web socket connected to {url}");
					connected = true;
				};
				_websocket.OnError += e =>
				{
					Debug.LogError("Web socket error! " + e);
					connected = false;
				};
				_websocket.OnClose += e =>
				{
					Debug.Log("Web socket closed! " + e);
					connected = false;
				};

				_websocket.Connect();

				var stopwatch = new Stopwatch();
				stopwatch.Start();
				while (!connected.HasValue && stopwatch.Elapsed < WebSocketHandshakeTimeout)
					yield return null;

				if (connected == true)
					break;

				_websocket = null;
				yield return new WaitForSeconds((float) WaitTimeToRetryConnect.TotalSeconds);
			}

			if (_websocket == null)
			{
				connectedCallback.Invoke(false);
				yield break;
			}

			var receiver = new WebSocketCommunicationWrapper(_websocket);
			_halfRemoteMatchClientAdapter.ConnectToServer(new ProtoNetworkDatagramClient(receiver), _userId, _useWebRtc);
			connectedCallback.Invoke(true);
		}

		public IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct)
		{
			connectedCallback?.Invoke(false);
			yield break;
		}

		public void Disconnect()
		{
			_halfRemoteMatchClientAdapter.PlayerDisconnected();
			_tcpClient?.Close();
			_websocket?.Close();
			DisconnectedByServer?.Invoke();
		}
	}
}
