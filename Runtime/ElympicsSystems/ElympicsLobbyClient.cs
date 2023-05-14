using System;
using System.Threading;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Plugins.Elympics.Plugins.ParrelSync;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable CS0618

namespace Elympics
{
	public class ElympicsLobbyClient : MonoBehaviour
	{
		public static ElympicsLobbyClient Instance { get; private set; }

		#region Authentication

		[SerializeField] private AuthType authenticateOnAwakeWith = AuthType.ClientSecret;
		[SerializeField] private ElympicsEthSigner ethSigner;

		// TODO: remove the following measures of backwards compatibility one day ~dsygocki 2023-04-28
		private AuthType AuthenticateOnAwakeWith => migratedAuthSettings
			? authenticateOnAwakeWith
			: authenticateOnAwake ? AuthType.ClientSecret : AuthType.None;
		[SerializeField, HideInInspector] private bool authenticateOnAwake = true;
		[SerializeField, HideInInspector] private bool migratedAuthSettings;
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (PrefabUtility.IsPartOfPrefabAsset(this) || migratedAuthSettings)
				return;
			Undo.RecordObject(this, $"Migrate auth settings from {nameof(ElympicsLobbyClient)}");
			if (!authenticateOnAwake)
				authenticateOnAwakeWith = AuthType.None;
			migratedAuthSettings = true;
			if (PrefabUtility.IsPartOfPrefabInstance(this))
				PrefabUtility.RecordPrefabInstancePropertyModifications(this);
		}
#endif

		public event Action<AuthData> AuthenticationSucceeded;
		public event Action<string> AuthenticationFailed;
		public AuthData AuthData { get; private set; }
		public Guid? UserGuid => AuthData?.UserId;
		public bool IsAuthenticated => AuthData != null;

		private IAuthClient _auth;
		private bool _authInProgress;
		private string _clientSecret;

		#endregion Authentication

		#region Deprecated authentication

		[Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
		public event Action<Result<AuthenticationData, string>> AuthenticatedGuid;
		[Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
		public delegate void AuthenticationCallback(bool success, string userId, string jwtToken, string error);
		[Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
		public event AuthenticationCallback Authenticated;
		[Obsolete("Use " + nameof(UserGuid) + " instead")]
		public string UserId => UserGuid?.ToString();

		#endregion Deprecated authentication

		#region Matchmaking

		public   IMatchmakerEvents       Matchmaker    => _matchmaker;
		public   MatchmakingFinishedData MatchDataGuid { get; private set; }
		internal JoinedMatchMode         MatchMode     { get; private set; }

		private MatchmakerClient _matchmaker;
		private bool             _matchmakingInProgress;
		private bool             _loadingSceneOnFinished;

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
			_config = ElympicsConfig.Load();
			_gameConfig = _config.GetCurrentGameConfig();

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

			if (AuthenticateOnAwakeWith != AuthType.None)
				AuthenticateWith(AuthenticateOnAwakeWith);
		}

		// ReSharper disable once MemberCanBePrivate.Global
		/// <summary>
		/// Performs standard authentication. Has to be run before joining an online match requiring client-secret auth type.
		/// Done automatically on awake depending on <see cref="authenticateOnAwakeWith"/> value.
		/// </summary>
		[Obsolete("Use " + nameof(AuthenticateWith) + " instead")]
		public void Authenticate() => AuthenticateWith(AuthType.ClientSecret);

		// ReSharper disable once MemberCanBePrivate.Global
		/// <summary>
		/// Performs authentication of specified type. Has to be run before joining an online match.
		/// Done automatically on awake depending on <see cref="authenticateOnAwakeWith"/> value.
		/// </summary>
		/// <param name="authType">Type of authentication to be performed.</param>
		public void AuthenticateWith(AuthType authType)
		{
			if (!Enum.IsDefined(typeof(AuthType), authType) || authType == AuthType.None)
			{
				Debug.LogError($"[Elympics] Invalid authentication type {authType}");
				return;
			}
			if (IsAuthenticated)
			{
				Debug.LogError($"[Elympics] User already authenticated (with {AuthData.AuthType} auth type)");
				return;
			}
			if (_authInProgress)
			{
				Debug.LogError($"[Elympics] Authentication already in progress");
				return;
			}

			_authInProgress = true;
			try
			{
				if (authType == AuthType.ClientSecret)
					_auth.AuthenticateWithClientSecret(_clientSecret, OnAuthenticatedWith(authType));
				else if (authType == AuthType.EthAddress)
					_auth.AuthenticateWithEthAddress(ethSigner, OnAuthenticatedWith(authType));
			}
			catch
			{
				_authInProgress = false;
				throw;
			}
		}

		private Action<Result<AuthData, string>> OnAuthenticatedWith(AuthType authType) => result =>
		{
			OnAuthenticatedWith(authType, result);
			_authInProgress = false;
		};

		private void OnAuthenticatedWith(AuthType authType, Result<AuthData, string> result)
		{
			if (result.IsSuccess)
			{
				AuthData = result.Value;
				Debug.Log($"[Elympics] {authType} authentication successful with user id: {AuthData.UserId}");
			}
			else
				Debug.LogError($"[Elympics] {authType} authentication failed: {result.Error}");

			string eventName = null;
			try
			{
				if (result.IsSuccess)
				{
					eventName = nameof(AuthenticationSucceeded);
					AuthenticationSucceeded?.Invoke(result.Value);
				}
				else
				{
					eventName = nameof(AuthenticationFailed);
					AuthenticationFailed?.Invoke(result.Error);
				}
				eventName = nameof(AuthenticatedGuid);
				AuthenticatedGuid?.Invoke(result.IsSuccess
					? Result<AuthenticationData, string>.Success(new AuthenticationData(result.Value))
					: Result<AuthenticationData, string>.Failure(result.Error));
				eventName = nameof(Authenticated);
				Authenticated?.Invoke(result.IsSuccess, result.Value.UserId.ToString(), result.Value.JwtToken, result.Error);
			}
			catch (Exception e)
			{
				Debug.LogException(new Exception($"Exception occured in one of listeners of {nameof(ElympicsLobbyClient)}.{eventName}", e));
			}
		}

		/// <summary>
		/// Resets the authentication state.
		/// After running this method, you have to authenticate using <see cref="AuthenticateWith"/> before joining an online match.
		/// </summary>
		public void SignOut()
		{
			if (_authInProgress)
			{
				Debug.LogError($"[Elympics] Authentication already in progress. Wait for it to complete before signing out");
				return;
			}
			if (!IsAuthenticated)
			{
				Debug.LogError($"[Elympics] User is not authenticated");
				return;
			}
			AuthData = null;
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


		[Obsolete("Please use " + nameof(PlayOnlineInRegion) + " instead.")]
		public void PlayOnline(float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true, string regionName = null, CancellationToken cancellationToken = default)
		{
			PlayOnlineInRegion(regionName, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);
		}

		/// <remarks>In a performance manner, it is better to use PlayOnlineInRegion if the region is known upfront. This method pings every Elympics region. If ping will fail, fallback region will be used.</remarks>
		public async void PlayOnlineInClosestRegionAsync(float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true, CancellationToken cancellationToken = default, string fallbackRegion = "warsaw")
		{
			if (string.IsNullOrEmpty(fallbackRegion))
				throw new ArgumentNullException(nameof(fallbackRegion));
			if (!CanJoinMatch())
				return;

			var closestRegion = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
			if (string.IsNullOrEmpty(closestRegion))
				closestRegion = fallbackRegion;
			SetupMatchAndJoinMatchmaker(closestRegion, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);
		}

		public void PlayOnlineInRegion(string regionName, float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, bool loadGameplaySceneOnFinished = true, CancellationToken cancellationToken = default)
		{
			if (!CanJoinMatch())
				return;
			SetupMatchAndJoinMatchmaker(regionName, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);
		}


		private void SetupMatchAndJoinMatchmaker(string regionName, float[] matchmakerData, byte[] gameEngineData, string queueName, bool loadGameplaySceneOnFinished, CancellationToken cancellationToken)
		{
			SetUpMatch(JoinedMatchMode.Online);
			_matchmakingInProgress = true;
			_loadingSceneOnFinished = loadGameplaySceneOnFinished;

			_matchmaker.JoinMatchmakerAsync(new JoinMatchmakerData
			{
				GameId = new Guid(_gameConfig.GameId),
				GameVersion = _gameConfig.GameVersion,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData,
				QueueName = queueName,
				RegionName = regionName,
			}, AuthData, cancellationToken);
		}

		private bool CanJoinMatch()
		{
			if (_matchmakingInProgress)
			{
				Debug.LogError("[Elympics] Joining match already in progress.");
				return false;
			}

			if (!IsAuthenticated)
			{
				Debug.LogError($"[Elympics] Cannot join match because user is not authenticated");
				return false;
			}

			return true;
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
			_matchmakingInProgress = false;
		}

		private void LoadGameplayScene() => SceneManager.LoadScene(_gameConfig.GameplayScene);


		private const  string ClientSecretPlayerPrefsKeyBase = "Elympics/AuthToken";
		private static string ClientSecretPlayerPrefsKey => ElympicsClonesManager.IsClone() ? $"{ClientSecretPlayerPrefsKeyBase}_clone_{ElympicsClonesManager.GetCloneNumber()}" : ClientSecretPlayerPrefsKeyBase;

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
