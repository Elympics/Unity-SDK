using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class ElympicsLobbyClient : MonoBehaviour
	{
		internal enum JoinedMatchMode
		{
			Online,
			Local,
			HalfRemoteClient,
			HalfRemoteServer
		}

		private const string AUTH_TOKEN_PLAYER_PREFS_KEY = "Elympics/AuthToken";

		public static ElympicsLobbyClient Instance { get; private set; }

#pragma warning disable IDE0044 // Add readonly modifier
		[SerializeField] private bool authenticateOnAwake = true;
#pragma warning restore IDE0044 // Add readonly modifier

		public delegate void AuthenticationCallback(bool success, string userId, string jwtToken, string error);

		public event AuthenticationCallback Authenticated;

		public   string                UserId          { get; set; }
		public   bool                  IsAuthenticated { get; private set; }
		public   JoinedMatchData       MatchData       { get; set; }
		public   IAuthenticationClient Auth            { get; private set; }
		public   IMatchmakerClient     Matchmaker      { get; private set; }
		internal JoinedMatchMode       MatchMode       { get; private set; }

		private ElympicsConfig     _config;
		private ElympicsGameConfig _gameConfig;
		private UserApiClient      _lobbyPublicApiClient;

		private string  _authToken;
		private float[] _matchmakerData;
		private byte[]  _gameEngineData;
		private string  _queueName;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
			SetAuthToken();
			LoadConfig();

			_lobbyPublicApiClient = new UserApiClient();
			Auth = new RemoteAuthenticationClient(_lobbyPublicApiClient);
			Matchmaker = new RemoteMatchmakerClient(_lobbyPublicApiClient);
			Matchmaker.MatchmakingFinished += SetMatchDataOnMatchmakingFinished;

			if (authenticateOnAwake)
				Authenticate();
		}

		private void SetMatchDataOnMatchmakingFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) result)
		{
			MatchData = new JoinedMatchData(result.MatchId, result.TcpUdpServerAddress, result.WebServerAddress, result.UserSecret, result.MatchedPlayers, _matchmakerData, _gameEngineData, _queueName);
		}

		private void LoadConfig()
		{
			_config = ElympicsConfig.Load();
		}

		public void Authenticate()
		{
			if (IsAuthenticated)
			{
				Debug.LogError("[Elympics] User already authenticated.");
				return;
			}

			var authenticationEndpoint = _config.ElympicsEndpoint;
			if (string.IsNullOrEmpty(authenticationEndpoint))
			{
				Debug.LogError($"[Elympics] Elympics authentication endpoint not set. Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}]");
				return;
			}

			Auth.AuthenticateWithAuthTokenAsync(authenticationEndpoint, _authToken, OnAuthenticated);
		}

		private void OnAuthenticated((bool Success, string UserId, string JwtToken, string Error) result)
		{
			if (result.Success)
			{
				Debug.Log($"[Elympics] Authentication successful with user id: {result.UserId}");
				IsAuthenticated = true;
				UserId = result.UserId;
			}
			else
			{
				Debug.LogError($"[Elympics] Authentication failed: {result.Error}");
			}

			Authenticated?.Invoke(result.Success, result.UserId, result.JwtToken, result.Error);
		}

		public void PlayOffline()
		{
			SetUpMatch(JoinedMatchMode.Local);
			LoadGameplayScene();
		}

		public void PlayHalfRemote(int playerId)
		{
			Environment.SetEnvironmentVariable(ApplicationParameters.HalfRemote.PlayerIndexEnvironmentVariable, playerId.ToString());
			SetUpMatch(JoinedMatchMode.HalfRemoteClient);
			LoadGameplayScene();
		}

		public void StartHalfRemoteServer()
		{
			SetUpMatch(JoinedMatchMode.HalfRemoteServer);
			LoadGameplayScene();
		}

		public void PlayOnline(float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true)
		{
			if (!IsAuthenticated)
			{
				Debug.LogError("[Elympics] User not authenticated, aborting join match.");
				return;
			}

			SetUpMatch(JoinedMatchMode.Online);

			_matchmakerData = matchmakerData;
			_gameEngineData = gameEngineData;
			_queueName = queueName;

			if (loadGameplaySceneOnFinished)
			{
				Matchmaker.MatchmakingFinished += HandleMatchmakingFinished;
				Matchmaker.MatchmakingFinished += _ => Debug.Log("Matchmaking finished successfully");
				Matchmaker.MatchmakingError += error => Debug.Log($"Matchmaking error - {error}");
			}

			Matchmaker.JoinMatchmakerAsync(_gameConfig.GameId, _gameConfig.GameVersion, _gameConfig.ReconnectEnabled, matchmakerData, gameEngineData, queueName, CancellationToken.None);
		}

		private void SetUpMatch(JoinedMatchMode mode)
		{
			_gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
			MatchMode = mode;
		}

		private void HandleMatchmakingFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) obj)
		{
			Matchmaker.MatchmakingFinished -= HandleMatchmakingFinished;
			LoadGameplayScene();
		}

		private void LoadGameplayScene()
			=> SceneManager.LoadScene(_gameConfig.GameplayScene);

		private void SetAuthToken()
		{
			if (!PlayerPrefs.HasKey(AUTH_TOKEN_PLAYER_PREFS_KEY))
				CreateNewAuthToken();
			_authToken = PlayerPrefs.GetString(AUTH_TOKEN_PLAYER_PREFS_KEY);
		}

		private static void CreateNewAuthToken()
		{
			PlayerPrefs.SetString(AUTH_TOKEN_PLAYER_PREFS_KEY, Guid.NewGuid().ToString());
			PlayerPrefs.Save();
		}
	}
}
