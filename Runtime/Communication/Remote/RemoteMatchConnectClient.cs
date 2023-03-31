using System;
using System.Collections;
using System.Threading;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;
using UnityEngine;

namespace Elympics
{
	public class RemoteMatchConnectClient : IMatchConnectClient
	{
		public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		public event Action                          ConnectingFailed;

		public event Action<string> AuthenticatedUserMatchWithUserId;
		public event Action<string> AuthenticatedUserMatchFailedWithError;

		public event Action         AuthenticatedAsSpectator;
		public event Action<string> AuthenticatedAsSpectatorWithError;

		public event Action<string> MatchJoinedWithError;
		public event Action<string> MatchJoinedWithMatchId;

		public event Action<string> MatchEndedWithMatchId;

		public event Action DisconnectedByServer;
		public event Action DisconnectedByClient;

		private readonly IGameServerClient   _gameServerClient;

		private readonly string _tcpUdpServerAddress;
		private readonly string _webServerAddress;
		private readonly string _userSecret;
		private readonly bool   _useWeb;

		private bool _connecting;
		private bool _connected;

		private Action _disconnectedCallback;
		private Action _matchJoinedCallback;

		public RemoteMatchConnectClient(IGameServerClient gameServerClient, string matchId, string tcpUdpServerAddress, string webServerAddress, string userSecret, bool useWeb = false, string regionName = null)
		{
			_gameServerClient = gameServerClient;
			_tcpUdpServerAddress = tcpUdpServerAddress;
			_webServerAddress = webServerAddress;
			_userSecret = userSecret;
			_useWeb = useWeb;
			_gameServerClient.Disconnected += OnDisconnectedByServer;
			_gameServerClient.MatchEnded += OnMatchEnded;
		}

		public IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)
		{
			CheckAddress();
			if (string.IsNullOrEmpty(_userSecret))
				throw new ArgumentNullException(nameof(_userSecret));
			return ConnectAndJoin(connectedCallback, SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer, ct);
		}

		private void CheckAddress()
		{
			if (_useWeb)
			{
				if (string.IsNullOrEmpty(_webServerAddress))
					throw new ArgumentNullException(nameof(_webServerAddress));
			}
			else
			{
				if (string.IsNullOrEmpty(_tcpUdpServerAddress))
					throw new ArgumentNullException(nameof(_tcpUdpServerAddress));
			}
		}

		public IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct)
		{
			CheckAddress();
			return ConnectAndJoin(connectedCallback, SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator, ct);
		}

		public void Disconnect()
		{
			if (!_connected)
				return;
			_connected = false;


			DisconnectedByClient?.Invoke();
			_gameServerClient.Disconnect();
		}

		private void SetupCallbacksForJoiningAsPlayer()
		{
			_gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsPlayer;
			_gameServerClient.UserMatchAuthenticated += OnAuthenticatedMatchUserSecret;
			_gameServerClient.MatchJoined += OnMatchJoined;
			_gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
		}

		private void UnsetCallbacksForJoiningAsPlayer()
		{
			_gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsPlayer;
			_gameServerClient.UserMatchAuthenticated -= OnAuthenticatedMatchUserSecret;
			_gameServerClient.MatchJoined -= OnMatchJoined;
			_gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
		}

		private void SetupCallbacksForJoiningAsSpectator()
		{
			_gameServerClient.ConnectedAndSynchronized += OnConnectedAndSynchronizedAsSpectator;
			_gameServerClient.AuthenticatedAsSpectator += OnAuthenticatedAsSpectator;
			_gameServerClient.MatchJoined += OnMatchJoined;
			_gameServerClient.Disconnected += OnDisconnectedWhileConnectingAndJoining;
		}

		private void UnsetCallbacksForJoiningAsSpectator()
		{
			_gameServerClient.ConnectedAndSynchronized -= OnConnectedAndSynchronizedAsSpectator;
			_gameServerClient.AuthenticatedAsSpectator -= OnAuthenticatedAsSpectator;
			_gameServerClient.MatchJoined -= OnMatchJoined;
			_gameServerClient.Disconnected -= OnDisconnectedWhileConnectingAndJoining;
		}

		private IEnumerator ConnectAndJoin(Action<bool> connectedCallback, Action setupCallbacks, Action unsetCallbacks, CancellationToken ct = default)
		{
			if (_connecting)
			{
				connectedCallback.Invoke(false);
				yield break;
			}

			_connecting = true;

			if (_connected)
			{
				connectedCallback.Invoke(false);
				yield break;
			}

			setupCallbacks();

			void ConnectedCallback(bool connected)
			{
				// Connect callback handled by setupCallbacks()
				if (connected)
					return;

				ConnectingFailed?.Invoke();
				connectedCallback?.Invoke(false);
			}

			void DisconnectedCallback()
			{
				FinishConnecting(unsetCallbacks);
				connectedCallback.Invoke(false);
			}

			void MatchJoinedCallback()
			{
				_connected = true;
				FinishConnecting(unsetCallbacks);
				connectedCallback.Invoke(true);
			}

			_disconnectedCallback = DisconnectedCallback;
			_matchJoinedCallback = MatchJoinedCallback;

			Debug.Log(_useWeb
				? $"[Elympics] Connecting to game server by WebSocket/WebRTC on {_webServerAddress}"
				: $"[Elympics] Connecting to game server by TCP/UDP on {_tcpUdpServerAddress}");

			_gameServerClient.ConnectAsync(ct).ContinueWith(t =>
			{
				if (t.IsFaulted)
					Debug.LogErrorFormat("[Elympics] Connecting exception\n{0}", t.Exception);
				else
					ConnectedCallback(t.Result);
			}, ct);
		}

		private void FinishConnecting(Action unsetCallbacks)
		{
			_connecting = false;
			TryDisconnectByServerIfNotConnected();
			unsetCallbacks();
			_disconnectedCallback = null;
			_matchJoinedCallback = null;
		}

		private void OnConnectedAndSynchronizedAsPlayer(TimeSynchronizationData timeSynchronizationData)
		{
			ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
			_gameServerClient.AuthenticateMatchUserSecretAsync(_userSecret);
		}

		private void OnConnectedAndSynchronizedAsSpectator(TimeSynchronizationData timeSynchronizationData)
		{
			ConnectedWithSynchronizationData?.Invoke(timeSynchronizationData);
			_gameServerClient.AuthenticateAsSpectatorAsync();
		}

		private void OnAuthenticatedMatchUserSecret(UserMatchAuthenticatedMessage message)
		{
			if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
			{
				AuthenticatedUserMatchFailedWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			AuthenticatedUserMatchWithUserId?.Invoke(message.UserId);

			_gameServerClient.JoinMatchAsync();
		}

		private void OnAuthenticatedAsSpectator(AuthenticatedAsSpectatorMessage message)
		{
			if (!message.AuthenticatedSuccessfully || !string.IsNullOrEmpty(message.ErrorMessage))
			{
				AuthenticatedAsSpectatorWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			AuthenticatedAsSpectator?.Invoke();

			_gameServerClient.JoinMatchAsync();
		}

		private void OnMatchJoined(MatchJoinedMessage message)
		{
			if (!string.IsNullOrEmpty(message.ErrorMessage))
			{
				MatchJoinedWithError?.Invoke(message.ErrorMessage);
				_gameServerClient.Disconnect();
				return;
			}

			MatchJoinedWithMatchId?.Invoke(message.MatchId);
			_matchJoinedCallback?.Invoke();
		}

		private void OnMatchEnded(MatchEndedMessage message) => MatchEndedWithMatchId?.Invoke(message.MatchId);

		private void OnDisconnectedWhileConnectingAndJoining()
		{
			_disconnectedCallback?.Invoke();
		}

		private void OnDisconnectedByServer()
		{
			if (_connecting)
				return;
			TryDisconnectByServerIfNotConnected();
		}

		private void TryDisconnectByServerIfNotConnected()
		{
			if (!_connected)
				return;
			if (_gameServerClient.IsConnected)
				return;
			DisconnectedByServer?.Invoke();
			_connected = false;
		}

		public void Dispose() => Disconnect();
	}
}
