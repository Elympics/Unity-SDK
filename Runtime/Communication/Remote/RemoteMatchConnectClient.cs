using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
		private readonly HttpSignalingClient _signalingClient;

		private readonly string _tcpUdpServerAddress;
		private readonly string _webServerAddress;
		private readonly string _userSecret;
		private readonly bool   _useWeb;

		private bool _connecting = false;
		private bool _connected  = false;

		private Action _disconnectedCallback;
		private Action _matchJoinedCallback;

		public RemoteMatchConnectClient(IGameServerClient gameServerClient, string matchId, string tcpUdpServerAddress, string webServerAddress, string userSecret, bool useWeb = false, string regionName = null)
		{
			_gameServerClient = gameServerClient;
			Debug.Log(matchId);
			var webSignalingEndpoint = GameServerClient.GetWebSignalingEndpoint(ElympicsConfig.Load().ElympicsGameServersEndpoint, webServerAddress, matchId);
			if (!string.IsNullOrEmpty(regionName))
			{
				var uriBuilder = new UriBuilder(webSignalingEndpoint);
				uriBuilder.Host = regionName + "-" + uriBuilder.Host;
				webSignalingEndpoint = uriBuilder.Uri;
			}
			_signalingClient = new HttpSignalingClient(webSignalingEndpoint);
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
			return ConnectAndJoin(connectedCallback, ct, SetupCallbacksForJoiningAsPlayer, UnsetCallbacksForJoiningAsPlayer);
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
			return ConnectAndJoin(connectedCallback, ct, SetupCallbacksForJoiningAsSpectator, UnsetCallbacksForJoiningAsSpectator);
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

		private IEnumerator ConnectAndJoin(Action<bool> connectedCallback, CancellationToken ct, Action setupCallbacks, Action unsetCallbacks)
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

			if (_useWeb)
			{
				Debug.Log($"[Elympics] Connecting to game server by WebSocket/WebRTC on {_webServerAddress}");

				IEnumerator<double> synchronizer = null;
				yield return EnumerateNestedWithSimpleTypes(_gameServerClient.ConnectWebAsync(_signalingClient, ConnectedCallback, s => synchronizer = s, ct));

				// Add timeout to this ~pprzestrzelski 18.02.2022
				while (_connecting)
					yield return 0;
				while (_connected && synchronizer != null && synchronizer.MoveNext())
					yield return new WaitForSeconds((float)synchronizer.Current);
			}
			else
			{
				Debug.Log($"[Elympics] Connecting to game server by TCP/UDP on {_tcpUdpServerAddress}");
				_gameServerClient.ConnectTcpUdpAsync(_tcpUdpServerAddress).ContinueWith(t =>
				{
					if (t.IsFaulted)
						Debug.LogErrorFormat("[Elympics] Connecting exception\n{0}", t.Exception);
					else
						ConnectedCallback(t.Result);
				}, ct);
			}
		}

		private IEnumerator EnumerateNestedWithSimpleTypes(IEnumerator enumerator)
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
					case double timeD:
						yield return new WaitForSeconds((float)timeD);
						break;
					case int timeI:
						yield return new WaitForSeconds(timeI);
						break;
					case IEnumerator nested:
						yield return EnumerateNestedWithSimpleTypes(nested);
						break;
					default:
						yield return enumerator.Current;
						break;
				}
			}
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
