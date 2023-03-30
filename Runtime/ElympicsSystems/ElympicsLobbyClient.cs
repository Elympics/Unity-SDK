using System;
using System.Collections.Generic;
using System.Threading;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable CS0618

namespace Elympics
{
	public class ElympicsLobbyClient : MonoBehaviour
	{
		public static ElympicsLobbyClient Instance { get; private set; }

		#region Authentication

		[SerializeField] private bool authenticateOnAwake = true;
		[SerializeField] private bool authenticateEthOnAwake;
		[SerializeField] private ElympicsEthSigner ethSigner;

		public event Action<Result<AuthenticationData, string>> AuthenticatedGuid;
		public event Action<AuthType, Result<AuthenticationData, string>> AuthenticatedWithType;
		public IReadOnlyDictionary<AuthType, AuthenticationData> AuthDataByType => _authDataByType;
		public bool IsAuthenticatedWith(AuthType authType) => _authDataByType.ContainsKey(authType);

		private readonly Dictionary<AuthType, AuthenticationData> _authDataByType = new Dictionary<AuthType, AuthenticationData>();
		private readonly HashSet<AuthType> _authInProgress = new HashSet<AuthType>();

		private AuthenticationData ClientSecretAuthData =>
			_authDataByType.TryGetValue(AuthType.ClientSecret, out var authData) ? authData : null;

		private string _clientSecret;
		private IAuthClient _auth;

		#endregion Authentication

		#region Deprecated authentication

		[Obsolete("Use " + nameof(AuthenticatedGuid) + " instead")]
		public delegate void AuthenticationCallback(bool success, string userId, string jwtToken, string error);
		[Obsolete("Use " + nameof(AuthenticatedGuid) + " instead")]
		public event AuthenticationCallback Authenticated;

		[Obsolete("Use " + nameof(AuthDataByType) + " instead")]
		public Guid? UserGuid => ClientSecretAuthData?.UserId;
		[Obsolete("Use " + nameof(AuthDataByType) + " instead")]
		public string UserId => UserGuid?.ToString();
		[Obsolete("Use " + nameof(IsAuthenticatedWith) + " instead")]
		public bool IsAuthenticated => ClientSecretAuthData != null;

		#endregion Deprecated authentication

		#region Matchmaking

		public IMatchmakerEvents Matchmaker => _matchmaker;
		public MatchmakingFinishedData MatchDataGuid { get; private set; }
		internal JoinedMatchMode MatchMode { get; private set; }
		internal AuthType MatchAuthType { get; private set; }

		private MatchmakerClient _matchmaker;
		private bool _matchmakingInProgress;
		private bool _loadingSceneOnFinished;

		#endregion Matchmaking

		#region Deprecated matchmaking

		[Obsolete("Use " + nameof(MatchDataGuid) + " instead")]
		public JoinedMatchData MatchData { get; private set; }

		#endregion Deprecated matchmaking

		private ElympicsConfig     _config;
		private ElympicsGameConfig _gameConfig;

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
			LoadConfig();

			if (string.IsNullOrEmpty(_config.ElympicsLobbyEndpoint))
			{
				Debug.LogError($"[Elympics] Elympics authentication endpoint not set. Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}]");
				return;
			}

			_auth = new RemoteAuthClient();
			_matchmaker = MatchmakerClientFactory.Create(_gameConfig, _config.ElympicsLobbyEndpoint);
			Matchmaker.MatchmakingSucceeded += HandleMatchmakingSucceeded;
			Matchmaker.MatchmakingMatchFound += HandleMatchIdReceived;
			Matchmaker.MatchmakingCancelledGuid += HandleMatchmakingCancelled;
			Matchmaker.MatchmakingFailed += HandleMatchmakingFailed;
			Matchmaker.MatchmakingWarning += HandleMatchmakingWarning;

			if (authenticateOnAwake)
				AuthenticateWith(AuthType.ClientSecret);
			if (authenticateEthOnAwake)
				AuthenticateWith(AuthType.EthAddress);
		}

		private void LoadConfig()
		{
			_config = ElympicsConfig.Load();
			_gameConfig = _config.GetCurrentGameConfig();
		}

		// ReSharper disable once MemberCanBePrivate.Global
		/// <summary>
		/// Performs standard authentication. Has to be run before joining an online match requiring client-secret auth type.
		/// Done automatically on awake if <see cref="authenticateOnAwake"/> is set.
		/// </summary>
		[Obsolete("Use " + nameof(AuthenticateWith) + " instead")]
		public void Authenticate() => AuthenticateWith(AuthType.ClientSecret);

		// ReSharper disable once MemberCanBePrivate.Global
		/// <summary>
		/// Performs authentication of specified type. Has to be run before joining an online match.
		/// Done automatically on awake if <see cref="authenticateOnAwake"/> or <see cref="authenticateEthOnAwake"/> is set.
		/// </summary>
		/// <param name="authType">Type of authentication to be performed.</param>
		public void AuthenticateWith(AuthType authType)
		{
			if (!Enum.IsDefined(typeof(AuthType), authType) || authType == AuthType.Unknown)
			{
				Debug.LogError($"[Elympics] Invalid authentication type {authType}");
				return;
			}
			if (IsAuthenticatedWith(authType))
			{
				Debug.LogError($"[Elympics] User already authenticated with {authType} auth type");
				return;
			}
			if (_authInProgress.Contains(authType))
			{
				Debug.LogError($"[Elympics] Authentication already in progress for type {authType}");
				return;
			}

			_authInProgress.Add(authType);
			try
			{
				if (authType == AuthType.ClientSecret)
					_auth.AuthenticateWithClientSecret(_clientSecret, OnAuthenticatedWith(authType));
				else if (authType == AuthType.EthAddress)
					_auth.AuthenticateWithEthAddress(ethSigner, OnAuthenticatedWith(authType));
			}
			catch
			{
				_authInProgress.Remove(authType);
				throw;
			}
		}

		private Action<Result<AuthenticationData, string>> OnAuthenticatedWith(AuthType authType) => result =>
		{
			OnAuthenticatedWith(authType, result);
			_authInProgress.Remove(authType);
		};

		private void OnAuthenticatedWith(AuthType authType, Result<AuthenticationData, string> result)
		{
			if (result.IsSuccess)
			{
				var authData = result.Value;
				_authDataByType[authType] = authData;
				Debug.Log($"[Elympics] {authType} authentication successful with user id: {authData.UserId}");
			}
			else
				Debug.LogError($"[Elympics] {authType} authentication failed: {result.Error}");

			string eventName = null;
			try
			{
				eventName = nameof(AuthenticatedWithType);
				AuthenticatedWithType?.Invoke(authType, result);
				eventName = nameof(AuthenticatedGuid);
				AuthenticatedGuid?.Invoke(result);
				eventName = nameof(Authenticated);
				Authenticated?.Invoke(result.IsSuccess, result.Value.UserId.ToString(), result.Value.JwtToken,
					result.Error
				);
			}
			catch (Exception e)
			{
				Debug.LogException(new Exception($"Exception occured in one of listeners of {nameof(ElympicsLobbyClient)}.{eventName}", e));
			}
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

		public void PlayOnline(float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true, string regionName = null, CancellationToken cancellationToken = default, AuthType authType = AuthType.ClientSecret)
		{
			if (_matchmakingInProgress)
			{
				Debug.LogError("[Elympics] Joining match already in progress");
				return;
			}
			if (!IsAuthenticatedWith(authType))
			{
				Debug.LogError($"[Elympics] Cannot join match because user is not authenticated with {authType}");
				return;
			}

			_matchmakingInProgress = true;
			SetUpMatch(JoinedMatchMode.Online, authType);
			_loadingSceneOnFinished = loadGameplaySceneOnFinished;

			_matchmaker.JoinMatchmakerAsync(new JoinMatchmakerData
			{
				GameId = new Guid(_gameConfig.GameId),
				GameVersion = _gameConfig.GameVersion,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData,
				QueueName = queueName,
				RegionName = regionName,
			}, _authDataByType[authType], cancellationToken);
		}

		private void SetUpMatch(JoinedMatchMode mode, AuthType authType = AuthType.Unknown)
		{
			MatchMode = mode;
			MatchAuthType = authType;
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
			_matchmakingInProgress = false;
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
