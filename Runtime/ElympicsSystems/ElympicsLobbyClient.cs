using System;
using System.Threading;
using Elympics.Models.Matchmaking;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class ElympicsLobbyClient : MonoBehaviour
	{
		public static ElympicsLobbyClient Instance { get; private set; }

		[SerializeField] private bool authenticateOnAwake = true;

		public delegate void AuthenticationCallback(bool success, string userId, string jwtToken, string error);
		[Obsolete("Use " + nameof(AuthenticatedGuid) + " instead")]
		public event AuthenticationCallback Authenticated;
		public event Action<Result<AuthenticationData, string>> AuthenticatedGuid;

		public IMatchmakerEvents Matchmaker => _matchmaker;

		public Guid? UserGuid => _authData?.UserId;
		public string UserId => UserGuid?.ToString();
		public bool IsAuthenticated => _authData != null;

		internal JoinedMatchMode MatchMode { get; private set; }

		public MatchmakingFinishedData MatchDataGuid { get; private set; }
		public JoinedMatchData MatchData { get; private set; }

		private string                _clientSecret;
		private IAuthenticationClient _auth;
		private AuthenticationData    _authData;
		private ElympicsConfig        _config;
		private ElympicsGameConfig    _gameConfig;
		private IUserApiClient        _lobbyPublicApiClient;
		private MatchmakerClient      _matchmaker;

		private bool _inProgress;
		private bool _loadingSceneOnFinished;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
			SetClientSecret();
			_config = ElympicsConfig.Load();
			_gameConfig = _config.GetCurrentGameConfig();

			_lobbyPublicApiClient = new UserApiClient();
			_auth = new RemoteAuthenticationClient(_lobbyPublicApiClient);
			_matchmaker = MatchmakerClientFactory.Create(_gameConfig, _lobbyPublicApiClient);
			_matchmaker.MatchmakingSucceeded += HandleMatchmakingSucceeded;
			_matchmaker.MatchmakingMatchFound += HandleMatchIdReceived;
			_matchmaker.MatchmakingCancelledGuid += HandleMatchmakingCancelled;
			_matchmaker.MatchmakingFailed += HandleMatchmakingFailed;
			_matchmaker.MatchmakingWarning += HandleMatchmakingWarning;

			if (authenticateOnAwake)
				Authenticate();
		}

		// ReSharper disable once MemberCanBePrivate.Global
		/// <summary>
		/// Performs authentication. Has to be run before joining an online match.
		/// Done automatically on awake if <see cref="authenticateOnAwake"/> is set.
		/// </summary>
		public void Authenticate()
		{
			if (IsAuthenticated)
			{
				Debug.LogError("[Elympics] User already authenticated.");
				return;
			}

			var authenticationEndpoint = _config.ElympicsLobbyEndpoint;
			if (string.IsNullOrEmpty(authenticationEndpoint))
			{
				Debug.LogError($"[Elympics] Elympics authentication endpoint not set. Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}]");
				return;
			}

			_auth.AuthenticateWithClientSecret(authenticationEndpoint, _clientSecret, OnAuthenticated);
		}

		private void OnAuthenticated((bool Success, Guid UserId, string JwtToken, string Error) result)
		{
			if (result.Success)
			{
				Debug.Log($"[Elympics] Authentication successful with user id: {result.UserId}");
				_authData = new AuthenticationData(result.UserId, result.JwtToken);
				AuthenticatedGuid?.Invoke(Result<AuthenticationData, string>.Success(_authData));
			}
			else
			{
				Debug.LogError($"[Elympics] Authentication failed: {result.Error}");
				AuthenticatedGuid?.Invoke(Result<AuthenticationData, string>.Failure(result.Error));
			}

			Authenticated?.Invoke(result.Success, result.UserId.ToString(), result.JwtToken, result.Error);
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

		public void PlayOnline(float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true, string regionName = null, CancellationToken cancellationToken = default)
		{
			if (_inProgress)
			{
				Debug.LogError("[Elympics] Joining match already in progress.");
				return;
			}
			if (!IsAuthenticated)
			{
				Debug.LogError("[Elympics] User not authenticated, aborting join match.");
				return;
			}

			SetUpMatch(JoinedMatchMode.Online);
			_loadingSceneOnFinished = loadGameplaySceneOnFinished;
			_inProgress = true;

			_matchmaker.JoinMatchmakerAsync(new JoinMatchmakerData
			{
				GameId = new Guid(_gameConfig.GameId),
				GameVersion = _gameConfig.GameVersion,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData,
				QueueName = queueName,
				RegionName = regionName
			}, cancellationToken);
		}

		private void SetUpMatch(JoinedMatchMode mode)
		{
			_gameConfig = _config.GetCurrentGameConfig();
			MatchMode = mode;
		}

		private void HandleMatchmakingCancelled(Guid _)
		{
			Debug.Log("Matchmaking cancelled");
			CleanUpAfterMatchmaking();
		}

		private void HandleMatchmakingFailed((string Error, Guid MatchId) args)
		{
			Debug.LogError($"Matchmaking error: {args.Error}");
			CleanUpAfterMatchmaking();
		}

		private static void HandleMatchmakingWarning((string Warning, Guid MatchId) args)
		{
			Debug.LogWarning($"Matchmaking warning: {args.Warning}");
		}

		private static void HandleMatchIdReceived(Guid matchId)
		{
			Debug.Log($"Received match id {matchId}");
		}

		private void HandleMatchmakingSucceeded(MatchmakingFinishedData matchData)
		{
			Debug.Log("Matchmaking finished successfully");
			MatchDataGuid = matchData;
			MatchData = new JoinedMatchData(matchData);
			CleanUpAfterMatchmaking();
			if (_loadingSceneOnFinished)
				LoadGameplayScene();
		}

		private void CleanUpAfterMatchmaking()
		{
			_inProgress = false;
		}

		private void LoadGameplayScene() => SceneManager.LoadScene(_gameConfig.GameplayScene);


		private const string ClientSecretPlayerPrefsKeyBase = "Elympics/AuthToken";
		private static string ClientSecretPlayerPrefsKey => ElympicsClonesManager.IsClone()
			? $"{ClientSecretPlayerPrefsKeyBase}_clone_{ElympicsClonesManager.GetCloneNumber()}"
			: ClientSecretPlayerPrefsKeyBase;

		private void SetClientSecret()
		{
			var key = ClientSecretPlayerPrefsKey;
			if (!PlayerPrefs.HasKey(key))
			{
				PlayerPrefs.SetString(key, CreateNewClientSecret());
				PlayerPrefs.Save();
			}
			_clientSecret = PlayerPrefs.GetString(key);
		}

		private static string CreateNewClientSecret()
		{
			return Guid.NewGuid().ToString();
		}


		internal enum JoinedMatchMode
		{
			Online,
			Local,
			HalfRemoteClient,
			HalfRemoteServer,
		}
	}
}
