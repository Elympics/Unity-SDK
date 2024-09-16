using System;
using System.Collections.Generic;
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

#nullable enable

#pragma warning disable CS0618

namespace Elympics
{
    [RequireComponent(typeof(AsyncEventsDispatcher))]
    public class ElympicsLobbyClient : MonoBehaviour, IMatchLauncher, IAuthManager
    {
        public static ElympicsLobbyClient? Instance { get; private set; }

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

        private IAuthClient _auth = null!;
        public AuthData? AuthData { get; private set; }
        public Guid? UserGuid => AuthData?.UserId;
        public bool IsAuthenticated => AuthData != null;
        private bool _connectionInProgress;
        private string? _clientSecret;
        private SessionConnectionFactory _sessionConnectionFactory = null!;

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

        private readonly Lazy<WebSocketSession> _webSocketSession = new(() => Instance!.CreateWebSocketSession());
        private readonly Lazy<RoomsClient> _roomsClient = new(() => Instance!.CreateRoomsClient());
        private readonly Lazy<IRoomsManager> _roomsManager = new(() => Instance!.CreateRoomsManager());

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

        [PublicAPI] public IReadOnlyCollection<string>? AvailableRegions { get; private set; }

        private IAvailableRegionRetriever _regionRetriever = null!;

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
            _config.CurrentGameSwitched += UniTask.Action(async () => await UpdateGameConfig());
            _gameConfig = _config.GetCurrentGameConfig() ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly.");
            _regionRetriever = new DefaultRegionRetriever();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _clientSecret = GetOrCreateClientSecret();

            ElympicsLogger.Log($"Initializing Elympics v{ElympicsConfig.SdkVersion} menu scene...\n" + "Available games:\n" + string.Join("\n", _config.AvailableGames.Select(game => $"{game.GameName} (ID: {game.GameId}), version {game.GameVersion}")));

            if (string.IsNullOrEmpty(_config.ElympicsLobbyEndpoint))
            {
                ElympicsLogger.LogError("Elympics authentication endpoint not set. " + $"Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}].");
                return;
            }

            _auth = new RemoteAuthClient(_config.ElympicsAuthEndpoint);
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
            return new WebSocketSession(asyncEventsDispatcher);
        }

        private RoomsClient CreateRoomsClient() => new()
        {
            Session = _webSocketSession.Value
        };

        private RoomsManager CreateRoomsManager()
        {
            var webSocketSession = _webSocketSession.Value;
            var roomsManager = new RoomsManager(this, _roomsClient.Value);
            webSocketSession.Disconnected += OnWebSocketDisconnected;
            return roomsManager;
        }
        private void OnWebSocketDisconnected(DisconnectionData _) => RoomsManager.Reset();

        private void OnDestroy()
        {
            if (_webSocketSession.IsValueCreated)
            {
                _webSocketSession.Value.Disconnected -= OnWebSocketDisconnected;
                _webSocketSession.Value.Dispose();
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
            if (data.AuthFromCacheData is null
                && data.AuthType is null
                && data.Region == null)
                throw new ArgumentNullException("All data parameters are null");


            if (_connectionInProgress)
                throw new ElympicsException("Connection already in progress");

            try
            {
                _connectionInProgress = true;
                var availableRegions = await _regionRetriever.GetAvailableRegions();
                _sessionConnectionFactory = new SessionConnectionFactory(new StandardRegionValidator(availableRegions));
                AvailableRegions = availableRegions;
                ElympicsLogger.Log("Start connecting to Elympics.");
                var authorizeToElympics = GetAuthStrategy(AuthData is not null);
                var authResult = await authorizeToElympics.Authorize(data);
                OnAuthenticatedWith(authResult);

                var lobbyConnection = GetConnectionStrategy(AuthData is not null, _webSocketSession.Value.IsConnected);
                var connectionDetails = _sessionConnectionFactory.CreateSessionConnectionDetails(_config.ElympicsWebSocketUrl, AuthData, _gameConfig, data.Region);
                await lobbyConnection.Connect(connectionDetails);
                currentRegion = connectionDetails.RegionName;
                await RoomsManager.CheckJoinedRoomStatus();
            }
            catch (Exception e)
            {
                AuthData = null;
                throw new ElympicsException(e.Message);
            }
            finally
            {
                _connectionInProgress = false;
            }
        }

        [PublicAPI]
        public void RegisterEthSigner(ElympicsEthSigner signer) => ethSigner = signer is not null ? signer : throw new ArgumentNullException(nameof(signer));

        private async UniTask LegacyAuth(AuthType authType) => await ConnectToElympicsAsync(new ConnectionData()
        {
            AuthType = authType
        });
        private void OnAuthenticatedWith(Result<AuthData, string> result)
        {
            string? eventName = null;
            try
            {
                if (result.IsSuccess)
                {
                    if (result.Value != null)
                    {
                        AuthData = result.Value;
                        ElympicsLogger.Log($"{result.Value.AuthType} authentication successful with user id: {AuthData.UserId} Nickname: {AuthData.Nickname}.");
                        eventName = nameof(AuthenticationSucceeded);
                        AuthenticationSucceeded?.Invoke(AuthData);
                    }
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
            if (result.IsFailure)
                throw new ElympicsException($"Authentication failed {result.Error}");
        }
        public void SignOut()
        {
            ElympicsLogger.Log("Trying to sign out...");

            if (_connectionInProgress)
                throw new ElympicsException("Connection already in progress. Wait for it to complete before signing out");

            if (!IsAuthenticated)
                throw new ElympicsException("User is not authenticated");

            AuthData = null;

            DisconnectFromLobby();

            ElympicsLogger.Log("Signed out.");
        }
        private void DisconnectFromLobby()
        {
            if (!_webSocketSession.Value.IsConnected)
                return;

            ElympicsLogger.Log($"Closing current websocket.");
            _webSocketSession.Value.Disconnect(DisconnectionReason.ClientRequest);
        }
        private async UniTask UpdateGameConfig()
        {
            _gameConfig = _config.GetCurrentGameConfig() ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly.");
            ElympicsLogger.Log($"Current game has been changed to {_gameConfig.GameName} (ID: {_gameConfig.GameId}).");

            try
            {
                if (AuthData is not null)
                {
                    var connectionStrategy = GetConnectionStrategy(true, _webSocketSession.Value.IsConnected);
                    var newSession = _sessionConnectionFactory.CreateSessionConnectionDetails(_config.ElympicsWebSocketUrl, AuthData, _gameConfig, new RegionData(currentRegion));
                    await connectionStrategy.Connect(newSession);
                }
            }
            catch (Exception ex)
            {
                _ = ElympicsLogger.LogException(ex);
            }
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

            var availableRegions = await ElympicsRegions.GetAvailableRegions();
            var closestRegion = fallbackRegion;

            if (availableRegions is not null)
                closestRegion = (await ElympicsCloudPing.ChooseClosestRegion(availableRegions)).Region;

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

        private AuthorizationStrategy GetAuthStrategy(bool isAuthorized) => isAuthorized switch
        {
            true => new AuthorizedStrategy(AuthData, _auth, _clientSecret, ethSigner, _telegramSigner),
            false => new UnauthorizedStrategy(_auth, _clientSecret, ethSigner, _telegramSigner),
        };

        private ConnectionStrategy GetConnectionStrategy(bool isAuthenticated, bool isConnected) => (isAuthenticated, isConnected) switch
        {
            (true, true) => new AuthorizedConnectedSocketConnectionStrategy(_webSocketSession.Value, _webSocketSession.Value.ConnectionDetails!.Value),
            (true, false) => new AuthorizedNotConnectedStrategy(_webSocketSession.Value),
            (false, _) => new UnauthorizedSocketConnectionStrategy(_webSocketSession.Value),
        };

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
