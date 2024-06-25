using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using JetBrains.Annotations;
using Plugins.Elympics.Plugins.ParrelSync;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using LobbyRoutes = Elympics.Lobby.Models.Routes;

#nullable enable

#pragma warning disable CS0618

namespace Elympics
{
    [RequireComponent(typeof(AsyncEventsDispatcher))]
    public class ElympicsLobbyClient : MonoBehaviour, IMatchLauncher, IAuthManager
    {
        public static ElympicsLobbyClient? Instance { get; private set; }

        internal static IAuthClient? AuthClientOverride = null;
        internal static WebSocketSession.WebSocketFactory WebSocketFactoryOverride = null;

        #region Authentication

        [SerializeField] private AsyncEventsDispatcher? asyncEventsDispatcher;
        [SerializeField] private AuthType authenticateOnAwakeWith = AuthType.ClientSecret;

        [SerializeField] private ElympicsEthSigner? ethSigner;
        private ITelegramSigner? _telegramSigner;


        // TODO: remove the following measures of backwards compatibility one day ~dsygocki 2023-04-28
        private AuthType AuthenticateOnAwakeWith => migratedAuthSettings
            ? authenticateOnAwakeWith
            : authenticateOnAwake
                ? AuthType.ClientSecret
                : AuthType.None;

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

        public event Action<AuthData>? AuthenticationSucceeded;
        public event Action<string>? AuthenticationFailed;
        public AuthData? AuthData { get; private set; }
        public Guid? UserGuid => AuthData?.UserId;
        public bool IsAuthenticated => AuthData != null;

        private IAuthClient _auth = null!;
        private bool _authInProgress;
        private string? _clientSecret;

        #endregion Authentication

        #region Deprecated authentication

        [Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
        [PublicAPI] public event Action<Result<AuthenticationData, string>>? AuthenticatedGuid;

        [Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
        [PublicAPI] public delegate void AuthenticationCallback(bool success, string userId, string jwtToken, string error);

        [Obsolete("Use " + nameof(AuthenticationSucceeded) + " or " + nameof(AuthenticationFailed) + " instead")]
        [PublicAPI] public event AuthenticationCallback? Authenticated;

        [Obsolete("Use " + nameof(UserGuid) + " instead")]
        [PublicAPI] public string? UserId => UserGuid?.ToString();

        #endregion Deprecated authentication

        [PublicAPI]
        public IWebSocketSession WebSocketSession => _webSocketSession.IsValueCreated ? _webSocketSession.Value : throw new InvalidOperationException($"The instance of {nameof(ElympicsLobbyClient)} has not been initialized correctly.");

        #region Rooms

        [PublicAPI]
        public IRoomsManager RoomsManager => _roomsManager.IsValueCreated ? _roomsManager.Value : throw new InvalidOperationException($"The instance of {nameof(ElympicsLobbyClient)} has not been initialized correctly.");

        private readonly Lazy<WebSocketSession> _webSocketSession = new(() => Instance.CreateWebSocketSession());
        private readonly Lazy<RoomsClient> _roomsClient = new(() => Instance.CreateRoomsClient());
        private readonly Lazy<RoomsManager> _roomsManager = new(() => Instance.CreateRoomsManager());

        #endregion Rooms

        #region Matchmaking

        [Obsolete("Use " + nameof(RoomsManager) + " instead")]
        [PublicAPI] public IMatchmakerEvents Matchmaker => _matchmaker;

        [PublicAPI]
        public MatchmakingFinishedData? MatchDataGuid
        {
            get => _matchDataGuid;
            private set
            {
                _matchDataGuid = value;
                MatchData = new JoinedMatchData(value);
            }
        }

        private MatchmakingFinishedData? _matchDataGuid;

        internal JoinedMatchMode MatchMode { get; private set; }

        private MatchmakerClient _matchmaker = null!;
        private bool _matchmakingInProgress;

        [Tooltip("Default starting value. The value can be changed at runtime using " + nameof(ElympicsLobbyClient) + "." + nameof(Instance) + "." + nameof(ShouldLoadGameplaySceneAfterMatchmaking) + " property.")]
        [SerializeField]
        private bool shouldLoadGameplaySceneAfterMatchmaking = true;

        public bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }
        public bool IsCurrentlyInMatch => gameObject.FindObjectsOfTypeOnScene<ElympicsBase>().Any();

        #endregion Matchmaking

        #region Deprecated matchmaking

        [Obsolete("Use " + nameof(MatchDataGuid) + " instead")]
        [PublicAPI] public JoinedMatchData? MatchData { get; private set; }

        #endregion Deprecated matchmaking

        [SerializeField] private string currentRegion = string.Empty;
        [PublicAPI] public string CurrentRegion => currentRegion;

        private ElympicsConfig _config = null!;
        private ElympicsGameConfig _gameConfig = null!;

        private void Awake()
        {
            if (!ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            if (Instance != null)
            {
                ElympicsLogger.LogWarning($"An instance of {nameof(ElympicsLobbyClient)} already exists. " + $"Destroying {gameObject} game object...");
                Destroy(gameObject);
                return;
            }

            if (asyncEventsDispatcher == null)
                throw new InvalidOperationException($"Serialized field {nameof(asyncEventsDispatcher)} cannot be null.");
            _config = ElympicsConfig.Load() ?? throw new InvalidOperationException($"No {nameof(ElympicsConfig)} instance found.");
            _config.CurrentGameSwitched += UpdateGameConfig;
            _gameConfig = _config.GetCurrentGameConfig() ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly.");

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _clientSecret = GetOrCreateClientSecret();

            ElympicsLogger.Log($"Initializing Elympics v{ElympicsConfig.SdkVersion} menu scene...\n" + "Available games:\n" + string.Join("\n", _config.AvailableGames.Select(game => $"{game.GameName} (ID: {game.GameId}), version {game.GameVersion}")));

            if (string.IsNullOrEmpty(_config.ElympicsLobbyEndpoint))
            {
                ElympicsLogger.LogError("Elympics authentication endpoint not set. " + $"Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}].");
                return;
            }

            _auth = AuthClientOverride ?? new RemoteAuthClient(_config.ElympicsAuthEndpoint);
            _matchmaker = new WebSocketMatchmakerClient(_config.ElympicsLobbyEndpoint);
            ShouldLoadGameplaySceneAfterMatchmaking = shouldLoadGameplaySceneAfterMatchmaking;
            _roomsManager.Value.Reset(); // calling Value initializes RoomsManager and its dependencies (RoomsClient, WebSocketSession) ~dsygocki 2023-12-06
            Matchmaker.MatchmakingSucceeded += HandleMatchmakingSucceeded;
            Matchmaker.MatchmakingMatchFound += HandleMatchIdReceived;
            Matchmaker.MatchmakingCancelledGuid += HandleMatchmakingCancelled;
            Matchmaker.MatchmakingFailed += HandleMatchmakingFailed;
            Matchmaker.MatchmakingWarning += HandleMatchmakingWarning;

            ElympicsLogger.Log($"Initialized {nameof(ElympicsLobbyClient)}.");

            if (AuthenticateOnAwakeWith != AuthType.None)
                ConnectToElympicsAsync(new ConnectionData()
                {
                    AuthType = authenticateOnAwakeWith
                }).Forget();
        }

        private WebSocketSession CreateWebSocketSession()
        {
            if (asyncEventsDispatcher == null)
                throw new InvalidOperationException($"Serialized reference cannot be null: {nameof(asyncEventsDispatcher)}");
            return new WebSocketSession(asyncEventsDispatcher, WebSocketFactoryOverride);
        }

        private RoomsClient CreateRoomsClient() => new()
        {
            Session = _webSocketSession.Value
        };

        private RoomsManager CreateRoomsManager()
        {
            var webSocketSession = _webSocketSession.Value;
            var roomsManager = new RoomsManager(this, _roomsClient.Value);
            webSocketSession.Disconnected += roomsManager.Reset;
            return roomsManager;
        }

        private void OnDestroy()
        {
            if (_webSocketSession.IsValueCreated)
                _webSocketSession.Value.Dispose();
        }

        private async UniTask<Result<AuthData, string>?> AuthenticateWithCachedData(CachedAuthData data)
        {
            if (data.CachedData is null)
                throw new ArgumentException($"{nameof(data.CachedData)} cannot be null.");

            var cachedData = data.CachedData;
            ElympicsLogger.Log($"Starting cached {cachedData.AuthType} authentication...");

            if (CanAuthenticate(cachedData.AuthType, out var failure) == false)
                return failure;

            _authInProgress = true;
            try
            {
                var isJwtTokenExpired = JwtTokenUtil.IsJwtExpired(cachedData.JwtToken);

                if (isJwtTokenExpired == false)
                {
                    var result = Result<AuthData, string>.Success(cachedData);
                    OnAuthenticatedWith(cachedData.AuthType, result);
                    return result;
                }
                else if (data.AutoRetryIfExpired)
                {
                    _authInProgress = false;
                    return await AuthenticateWithAsync(cachedData.AuthType);
                }
                else
                {
                    var result = Result<AuthData, string>.Failure("Jwt token has expired.");
                    OnAuthenticatedWith(cachedData.AuthType, result);
                    return result;
                }
            }
            finally
            {
                _authInProgress = false;
            }
        }

        /// <summary>
        /// Performs standard authentication. Has to be run before joining an online match requiring client-secret auth type.
        /// Done automatically on awake depending on <see cref="authenticateOnAwakeWith"/> value.
        /// </summary>
        [Obsolete("Use " + nameof(ConnectToElympicsAsync) + " instead")]
        [PublicAPI]
        public void Authenticate() => LegacyAuth(AuthType.ClientSecret).Forget();

        [Obsolete("Use " + nameof(ConnectToElympicsAsync) + " instead")]
        [PublicAPI]
        public void AuthenticateWith(AuthType authType) => LegacyAuth(authType).Forget();

        public async UniTask ConnectToElympicsAsync(ConnectionData data)
        {
            var regionReconnectionRequested = data.AuthFromCacheData is null && data.AuthType is null && data.Region is not null;

            if (regionReconnectionRequested)
            {
                var region = data.Region!.Value;
                if (currentRegion == region.Name)
                {
                    ElympicsLogger.LogWarning($"New region {region.Name} is the same as currentRegion {currentRegion}.");
                    return;
                }

                if (string.IsNullOrEmpty(region.Name))
                    throw new ElympicsException("New region cannot be null or empty.");

                await ChangeRegionAndReconnectToLobby(region);
            }
            else
            {
                if (data.Region is not null)
                {
                    var region = data.Region.Value;
                    ThrowIfRegionValidationFailed(region);
                    currentRegion = region.Name;
                }

                Result<AuthData, string>? result = null;

                if (data.AuthFromCacheData is not null)
                    result = await AuthenticateWithCachedData(data.AuthFromCacheData.Value);
                else if (data.AuthType is not null)
                    result = await AuthenticateWithAsync(data.AuthType.Value);

                if (result is not null)
                {
                    if (result.IsSuccess)
                    {
                        await ConnectToLobby();
                        await RoomsManager.CheckJoinedRoomStatus();
                    }
                    else
                        ElympicsLogger.LogError(result.Error);
                }
            }
        }

        [PublicAPI]
        public void RegisterEthSigner(ElympicsEthSigner signer) => ethSigner = signer is not null ? signer : throw new ArgumentNullException(nameof(signer));

        private async UniTask LegacyAuth(AuthType authType) => await ConnectToElympicsAsync(new ConnectionData()
        {
            AuthType = authType
        });

        private async UniTask ChangeRegionAndReconnectToLobby(RegionData regionData)
        {
            ThrowIfRegionValidationFailed(regionData);
            currentRegion = regionData.Name;
            if (RoomsManager.ListJoinedRooms().Count > 0)
                ElympicsLogger.LogWarning("It is recommended to disconnect user from rooms before reconnecting to new region.");
            await ConnectToLobby();
        }

        private async UniTask<Result<AuthData, string>?> AuthenticateWithAsync(AuthType authType)
        {
            ElympicsLogger.Log($"Starting {authType} authentication...");

            if (CanAuthenticate(authType, out var failure) == false)
                return failure;

            _authInProgress = true;
            Result<AuthData, string>? authResult = null;
            try
            {
                switch (authType)
                {
                    case AuthType.ClientSecret:
                        authResult = await _auth.AuthenticateWithClientSecret(_clientSecret);
                        break;
                    case AuthType.EthAddress:
                        authResult = await _auth.AuthenticateWithEthAddress(ethSigner);
                        break;
                    case AuthType.Telegram:
                        authResult = await _auth.AuthenticateWithTelegram(_telegramSigner);
                        break;
                    case AuthType.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(authType), authType, null);
                }
                if (authResult != null)
                    OnAuthenticatedWith(authType, authResult);
                _authInProgress = false;
                return authResult;
            }
            catch
            {
                _authInProgress = false;
                throw;
            }
        }

        private void OnAuthenticatedWith(AuthType authType, Result<AuthData, string> result)
        {
            if (result.IsSuccess)
            {
                AuthData = result.Value;
                ElympicsLogger.Log($"{authType} authentication successful with user id: {AuthData.UserId} Nickname: {AuthData.Nickname}.");
            }
            else
                ElympicsLogger.LogError($"{authType} authentication failed: {result.Error}");

            string? eventName = null;
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
                AuthenticatedGuid?.Invoke(result.IsSuccess ? Result<AuthenticationData, string>.Success(new AuthenticationData(result.Value)) : Result<AuthenticationData, string>.Failure(result.Error));
                eventName = nameof(Authenticated);
                Authenticated?.Invoke(result.IsSuccess, result.Value?.UserId.ToString() ?? "", result.Value?.JwtToken ?? "", result.Error);
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException($"Exception occured in one of listeners of {nameof(ElympicsLobbyClient)}.{eventName}", e);
            }
        }

        public void SignOut()
        {
            ElympicsLogger.Log("Trying to sign out...");

            if (_authInProgress)
            {
                ElympicsLogger.LogError("Authentication already in progress. Wait for it to complete before signing out");
                return;
            }

            if (!IsAuthenticated)
            {
                ElympicsLogger.LogError("User is not authenticated");
                return;
            }

            AuthData = null;
            DisconnectFromLobby();

            ElympicsLogger.Log("Signed out.");
        }

        private void UpdateGameConfig()
        {
            _gameConfig = _config.GetCurrentGameConfig() ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly.");
            ElympicsLogger.Log($"Current game has been changed to {_gameConfig.GameName} (ID: {_gameConfig.GameId}).");

            ConnectToLobby().Forget();
        }

        private static void ThrowIfRegionValidationFailed(RegionData regionData)
        {
            if (regionData.IsCustom is false
                && ElympicsRegions.AllAvailableRegions.Contains(regionData.Name) is false)
                throw new ArgumentException($"The specified region must be one of the available regions listed in {nameof(ElympicsRegions.AllAvailableRegions)}.");
        }

        private async UniTask ConnectToLobby()
        {
            ElympicsLogger.Log("Connecting to lobby...");
            DisconnectFromLobby();
            if (!IsAuthenticated)
            {
                ElympicsLogger.Log("Connecting canceled because user is not authenticated.");
                return;
            }

            var connectionDetails = new SessionConnectionDetails(_config.ElympicsLobbyEndpoint.AppendPathSegments(LobbyRoutes.Base).ToString(), AuthData!, new Guid(_gameConfig.GameId), _gameConfig.GameVersion, currentRegion);
            try
            {
                await _webSocketSession.Value.Connect(connectionDetails);
                ElympicsLogger.Log("Successfully connected to lobby.\n Connection details: {connectionDetails}");
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }

        private void DisconnectFromLobby()
        {
            if (!_webSocketSession.Value.IsConnected)
                return;

            ElympicsLogger.Log($"Closing current websocket.");
            _webSocketSession.Value.Disconnect();
        }


        private void LogSettingUpGame(string gameModeName) =>
            ElympicsLogger.Log($"Setting up {gameModeName} mode for {_gameConfig.GameName} (ID: {_gameConfig.GameId}), version {_gameConfig.GameVersion}");

        [PublicAPI]
        public void PlayOffline()
        {
            LogSettingUpGame("Local Player with Bots");

            SetUpMatch(JoinedMatchMode.Local);
            LoadGameplayScene();
        }

        [PublicAPI]
        public void PlayHalfRemote(int playerId)
        {
            LogSettingUpGame("Half Remote Client");

            _gameConfig.PlayerIndexForHalfRemoteMode = playerId;
            SetUpMatch(JoinedMatchMode.HalfRemoteClient);
            LoadGameplayScene();
        }

        [PublicAPI]
        public void StartHalfRemoteServer()
        {
            LogSettingUpGame("Half Remote Server");

            SetUpMatch(JoinedMatchMode.HalfRemoteServer);
            LoadGameplayScene();
        }


        [Obsolete("Please use " + nameof(PlayOnlineInRegion) + " instead.")]
        [PublicAPI]
        public void PlayOnline(
            float[]? matchmakerData = null,
            byte[]? gameEngineData = null,
            string? queueName = null,
            bool loadGameplaySceneOnFinished = true,
            string? regionName = null,
            CancellationToken cancellationToken = default) => PlayOnlineInRegion(regionName, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);

        /// <remarks>In a performance manner, it is better to use PlayOnlineInRegion if the region is known upfront. This method pings every Elympics region. If ping will fail, fallback region will be used.</remarks>
        [Obsolete("Use " + nameof(RoomsManager) + " to set up matches instead")]
        [PublicAPI]
        public async void PlayOnlineInClosestRegionAsync(
            float[]? matchmakerData = null,
            byte[]? gameEngineData = null,
            string? queueName = null,
            bool loadGameplaySceneOnFinished = true,
            CancellationToken cancellationToken = default,
            string fallbackRegion = "warsaw")
        {
            LogSettingUpGame("Online (in closest region)");

            if (string.IsNullOrEmpty(fallbackRegion))
                throw new ArgumentNullException(nameof(fallbackRegion));
            if (!CanJoinMatch())
                return;

            var closestRegion = (await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions)).Region;
            if (string.IsNullOrEmpty(closestRegion))
                closestRegion = fallbackRegion;
            SetupMatchAndJoinMatchmaker(closestRegion, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);
        }

        [PublicAPI]
        [Obsolete("Use " + nameof(RoomsManager) + " to set up matches instead")]
        public void PlayOnlineInRegion(
            string? regionName,
            float[]? matchmakerData = null,
            byte[]? gameEngineData = null,
            string? queueName = null,
            bool loadGameplaySceneOnFinished = true,
            CancellationToken cancellationToken = default)
        {
            LogSettingUpGame("Online");

            if (!CanJoinMatch())
                return;
            SetupMatchAndJoinMatchmaker(regionName, matchmakerData, gameEngineData, queueName, loadGameplaySceneOnFinished, cancellationToken);
        }

        [PublicAPI]
        public void PlayMatch(MatchmakingFinishedData matchData)
        {
            if (IsCurrentlyInMatch)
                throw new InvalidOperationException("Game is already on the gameplay scene.");
            MatchDataGuid = matchData ?? throw new ArgumentNullException(nameof(matchData));
            LoadGameplayScene();
        }

        internal static void LogJoiningMatchmaker(
            Guid userId,
            float[]? matchmakerData,
            byte[]? gameEngineData,
            string? queueName,
            string? regionName,
            bool loadGameplaySceneOnFinished)
        {
            var serializedMmData = matchmakerData != null ? "[" + string.Join(", ", matchmakerData.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]" : "null";
            var serializedGeData = gameEngineData != null ? Convert.ToBase64String(gameEngineData) : "null";
            ElympicsLogger.Log($"Starting matchmaking process for user: {userId}, region: {regionName}, queue: {queueName}\nSupplied matchmaker data: {serializedMmData}\n" + $"Supplied game engine data: {serializedGeData}");
            if (loadGameplaySceneOnFinished)
                ElympicsLogger.Log("Gameplay scene will be loaded after matchmaking succeeds.");
        }

        private void SetupMatchAndJoinMatchmaker(
            string? regionName,
            float[]? matchmakerData,
            byte[]? gameEngineData,
            string? queueName,
            bool loadGameplaySceneOnFinished,
            CancellationToken cancellationToken)
        {
            ElympicsLogTemplates.LogJoiningMatchmaker(UserGuid!.Value, matchmakerData, gameEngineData, queueName, regionName, loadGameplaySceneOnFinished);

            SetUpMatch(JoinedMatchMode.Online);
            _matchmakingInProgress = true;
            ShouldLoadGameplaySceneAfterMatchmaking = loadGameplaySceneOnFinished;

            _matchmaker.JoinMatchmakerAsync(new JoinMatchmakerData
            {
                GameId = new Guid(_gameConfig.GameId),
                GameVersion = _gameConfig.GameVersion,
                MatchmakerData = matchmakerData,
                GameEngineData = gameEngineData,
                QueueName = queueName,
                RegionName = regionName
            },
            AuthData,
            cancellationToken);
        }

        [PublicAPI]
        public void HasAnyUnfinishedMatch(Action<bool> onSuccess, Action<string>? onFailure = null) => _matchmaker.CheckForAnyUnfinishedMatch(new Guid(_gameConfig.GameId), _gameConfig.GameVersion, AuthData, onSuccess, e => onFailure?.Invoke(e.Message));

        [PublicAPI]
        public void RejoinLastOnlineMatch(bool loadGameplaySceneOnFinished = true, CancellationToken ct = default)
        {
            ElympicsLogger.Log("Rejoining last Online game...");
            if (loadGameplaySceneOnFinished)
                ElympicsLogger.Log("Gameplay scene will be loaded after rejoining succeeds.");

            if (!CanJoinMatch())
                return;

            SetUpMatch(JoinedMatchMode.Online);
            _matchmakingInProgress = true;
            ShouldLoadGameplaySceneAfterMatchmaking = loadGameplaySceneOnFinished;

            _matchmaker.RejoinLastMatchAsync(new Guid(_gameConfig.GameId), _gameConfig.GameVersion, AuthData, ct);
        }

        private bool CanJoinMatch()
        {
            if (_matchmakingInProgress)
            {
                ElympicsLogger.LogError("Joining match already in progress.");
                return false;
            }

            if (!IsAuthenticated)
            {
                ElympicsLogger.LogError($"Cannot join match because user is not authenticated.");
                return false;
            }

            return true;
        }

        private void SetUpMatch(JoinedMatchMode mode) => MatchMode = mode;

        private void HandleMatchmakingCancelled(Guid _)
        {
            ElympicsLogger.Log("Matchmaking cancelled.");
            CleanUpAfterMatchmaking();
        }

        private void HandleMatchmakingFailed((string Error, Guid MatchId) args)
        {
            ElympicsLogger.LogError($"Matchmaking error: {args.Error}");
            CleanUpAfterMatchmaking();
        }

        private static void HandleMatchmakingWarning((string Warning, Guid MatchId) args) => ElympicsLogger.LogWarning($"Matchmaking warning: {args.Warning}");

        private static void HandleMatchIdReceived(Guid matchId) => ElympicsLogger.Log($"Received match ID: {matchId}.");

        private void HandleMatchmakingSucceeded(MatchmakingFinishedData matchData)
        {
            ElympicsLogger.Log("Matchmaking finished successfully.");
            MatchDataGuid = matchData;
            CleanUpAfterMatchmaking();
            if (ShouldLoadGameplaySceneAfterMatchmaking)
                LoadGameplayScene();
        }

        private void CleanUpAfterMatchmaking() => _matchmakingInProgress = false;

        private void LoadGameplayScene() => SceneManager.LoadScene(_gameConfig.GameplayScene);


        private const string ClientSecretPlayerPrefsKeyBase = "Elympics/AuthToken";
        private static string ClientSecretPlayerPrefsKey => ElympicsClonesManager.IsClone() ? $"{ClientSecretPlayerPrefsKeyBase}_clone_{ElympicsClonesManager.GetCloneNumber()}" : ClientSecretPlayerPrefsKeyBase;

        internal static string GetOrCreateClientSecret()
        {
            var parameterValue = ApplicationParameters.Parameters.ClientSecret.GetValue();
            if (!string.IsNullOrEmpty(parameterValue))
                return parameterValue;

            if (parameterValue == "")
                return CreateNewClientSecret();

            var key = ClientSecretPlayerPrefsKey;
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetString(key, CreateNewClientSecret());
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(key);
        }

        private bool CanAuthenticate(AuthType authType, out Result<AuthData, string>? failure)
        {
            if (!Enum.IsDefined(typeof(AuthType), authType)
                || authType == AuthType.None)
            {
                failure = Result<AuthData, string>.Failure($"Invalid authentication type: {authType}.");
                return false;
            }

            if (IsAuthenticated)
            {
                failure = Result<AuthData, string>.Failure($"User already authenticated (with {AuthData!.AuthType} auth type).");
                return false;
            }

            if (_authInProgress)
            {
                failure = Result<AuthData, string>.Failure("Authentication already in progress.");
                return false;
            }
            failure = null;
            return true;
        }

        private static string CreateNewClientSecret() => Guid.NewGuid().ToString();

        internal enum JoinedMatchMode
        {
            Online,
            Local,
            HalfRemoteClient,
            HalfRemoteServer,
        }
    }
}
