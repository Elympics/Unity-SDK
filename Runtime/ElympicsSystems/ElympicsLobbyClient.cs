using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using JetBrains.Annotations;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

#pragma warning disable CS0618

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.ElympicsLobbyClient)]
    [RequireComponent(typeof(AsyncEventsDispatcher))]
    public partial class ElympicsLobbyClient : MonoBehaviour, IMatchLauncher, IAuthManager
    {
        public static ElympicsLobbyClient? Instance { get; private set; }
        internal ElympicsLobbyClientState CurrentState { get; private set; } = null!;

        private ElympicsLobbyClientState? _previousState;

        private readonly Dictionary<ElympicsState, ElympicsLobbyClientState> _states = new();

        internal event Action<ElympicsState, ElympicsState>? StateChanged;

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

        public event Action<AuthData>? AuthenticationSucceeded;
        public event Action<string>? AuthenticationFailed;

        private IAuthClient _auth = null!;
        public AuthData? AuthData { get; internal set; }
        public Guid? UserGuid => AuthData?.UserId;
        public bool IsAuthenticated => AuthData != null;
        private string? _clientSecret;
        private SessionConnectionFactory _sessionConnectionFactory = null!;

        #endregion Authentication

        [PublicAPI]
        public IGameplaySceneMonitor GameplaySceneMonitor { get; private set; } = null!;

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

        [Tooltip("Default starting value. The value can be changed at runtime using " + nameof(ElympicsLobbyClient) + "." + nameof(Instance) + "." + nameof(ShouldLoadGameplaySceneAfterMatchmaking) + " property.")]
        [SerializeField]
        private bool shouldLoadGameplaySceneAfterMatchmaking = true;

        public bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        #endregion Matchmaking

        [SerializeField] private string currentRegion = string.Empty;

        [PublicAPI]
        public string CurrentRegion => currentRegion;

        [PublicAPI]
        public IReadOnlyCollection<string>? AvailableRegions { get; private set; }

        private IAvailableRegionRetriever _regionRetriever = null!;

        private ElympicsConfig _config = null!;
        private ElympicsGameConfig _gameConfig = null!;

        internal static ElympicsLoggerContext LoggerContext;
        private void Awake()
        {
            if (Instance != null)
            {
                ElympicsLogger.LogWarning($"An instance of {nameof(ElympicsLobbyClient)} already exists. " + $"Destroying {gameObject} game object...");
                Destroy(gameObject);
                return;
            }


            if (!ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            if (asyncEventsDispatcher == null)
                throw LoggerContext.CaptureAndThrow(new InvalidOperationException($"Serialized field {nameof(asyncEventsDispatcher)} cannot be null."));

            _config = ElympicsConfig.Load() ?? throw LoggerContext.CaptureAndThrow(new InvalidOperationException($"No {nameof(ElympicsConfig)} instance found."));

            _config.CurrentGameSwitched += UniTask.Action(async () => await UpdateGameConfig());
            _gameConfig = _config.GetCurrentGameConfig() ?? throw LoggerContext.CaptureAndThrow(new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly."));
            LoggerContext = new ElympicsLoggerContext(ElympicsLogger.SessionId, ElympicsConfig.SdkVersion, _gameConfig.GameId)
            {
                Context = nameof(ElympicsLobbyClient),
            }.WithApp(ElympicsLoggerContext.ElympicsContextApp);
            _regionRetriever = new DefaultRegionRetriever();

            var awakeLogger = LoggerContext.WithMethodName();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _clientSecret = GetOrCreateClientSecret();

            awakeLogger.Log($"Initializing Elympics menu scene... {Environment.NewLine} Available games: {Environment.NewLine} {string.Join($"{Environment.NewLine}", _config.AvailableGames.Select(game => $"{game.GameName} (ID: {game.GameId}), version {game.GameVersion}"))}");

            if (string.IsNullOrEmpty(_config.ElympicsLobbyEndpoint))
                throw awakeLogger.CaptureAndThrow(new ArgumentException($"Elympics authentication endpoint not set. Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}].", nameof(_config.ElympicsAuthEndpoint)));

            _auth = new RemoteAuthClient(_config.ElympicsAuthEndpoint);
            _matchmaker = new WebSocketMatchmakerClient(_config.ElympicsLobbyEndpoint);
            GameplaySceneMonitor = new GameplaySceneMonitor(_gameConfig.gameplayScene);
            GameplaySceneMonitor.GameplayFinished += OnGameplayFinished;
            ShouldLoadGameplaySceneAfterMatchmaking = shouldLoadGameplaySceneAfterMatchmaking;
            _roomsManager.Value.Reset(); // calling Value initializes RoomsManager and its dependencies (RoomsClient, WebSocketSession) ~dsygocki 2023-12-06
            Matchmaker.MatchmakingSucceeded += HandleMatchmakingSucceeded;
            Matchmaker.MatchmakingMatchFound += HandleMatchIdReceived;
            Matchmaker.MatchmakingCancelledGuid += HandleMatchmakingCancelled;
            Matchmaker.MatchmakingFailed += HandleMatchmakingFailed;
            Matchmaker.MatchmakingWarning += HandleMatchmakingWarning;

            awakeLogger.Log("Initialized");

            CurrentState = new DisconnectedState(this);
            if (AuthenticateOnAwakeWith != AuthType.None)
                ConnectToElympicsAsync(new ConnectionData()
                {
                    AuthType = authenticateOnAwakeWith
                }).Forget();
        }

        #region Public API

        [PublicAPI]
        public async UniTask ConnectToElympicsAsync(ConnectionData data)
        {
            try
            {
                var logger = LoggerContext.WithMethodName();
                await CurrentState.Connect(data);
            }
            catch (Exception e)
            {
                AuthData = null;
                throw LoggerContext.CaptureAndThrow(e);
            }
        }

        [PublicAPI]
        public void RegisterEthSigner(ElympicsEthSigner signer) => ethSigner = signer is not null ? signer : throw new ArgumentNullException(nameof(signer));

        [PublicAPI]
        public void SignOut() => CurrentState.SignOut();

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

        [PublicAPI]
        public void PlayMatch(MatchmakingFinishedData matchData) => CurrentState.PlayMatch(matchData);

        #endregion

        #region private methods

        private void OnAuthenticatedWith(Result<AuthData, string> result)
        {
            var logger = LoggerContext.WithMethodName();
            string? eventName = null;
            try
            {
                if (result.IsSuccess)
                {
                    if (result.Value != null)
                    {
                        AuthData = result.Value;
                        logger.SetUserId(result.Value.UserId.ToString()).SetNickname(result.Value.Nickname).SetAuthType(result.Value.AuthType).Log("Authentication completed.");
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
                logger.Exception(new ElympicsException($"Exception occured in one of listeners of {nameof(ElympicsLobbyClient)}.{eventName}", e));
            }
            if (result.IsFailure)
                throw logger.CaptureAndThrow(new ElympicsException($"Authentication failed {result.Error}"));
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
            GameplaySceneMonitor!.GameConfigChanged(_gameConfig.gameplayScene);

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
        private void LogSettingUpGame(string gameModeName) => LoggerContext.Log($"Setting up {gameModeName} mode for {_gameConfig.GameName} (ID: {_gameConfig.GameId}), version {_gameConfig.GameVersion}");

        private AuthorizationStrategy GetAuthStrategy(bool isAuthorized) => isAuthorized switch
        {
            true => new AuthorizedStrategy(AuthData, _auth, _clientSecret, ethSigner, _telegramSigner),
            false => new UnauthorizedStrategy(_auth, _clientSecret, ethSigner, _telegramSigner),
        };

        private ConnectionStrategy GetConnectionStrategy(bool isAuthenticated, bool isConnected) => (isAuthenticated, isConnected) switch
        {
            (true, true) => new AuthorizedConnectedSocketConnectionStrategy(_webSocketSession.Value, _webSocketSession.Value.ConnectionDetails!.Value, LoggerContext),
            (true, false) => new AuthorizedNotConnectedStrategy(_webSocketSession.Value, LoggerContext),
            (false, _) => new UnauthorizedSocketConnectionStrategy(_webSocketSession.Value, LoggerContext),
        };


        private void SetUpMatch(JoinedMatchMode mode) => MatchMode = mode;

        private void LoadGameplayScene() => SceneManager.LoadScene(_gameConfig.GameplayScene);

        private const string ClientSecretPlayerPrefsKeyBase = "Elympics/AuthToken";
        private static string ClientSecretPlayerPrefsKey => ElympicsClonesManager.IsClone() ? $"{ClientSecretPlayerPrefsKeyBase}_clone_{ElympicsClonesManager.GetCloneNumber()}" : ClientSecretPlayerPrefsKeyBase;

        private static string CreateNewClientSecret() => Guid.NewGuid().ToString();

        private void OnDestroy()
        {
            if (_webSocketSession.IsValueCreated)
            {
                _webSocketSession.Value.Disconnected -= OnWebSocketDisconnected;
                _webSocketSession.Value.Dispose();
            }
            GameplaySceneMonitor?.Dispose();
        }

        private WebSocketSession CreateWebSocketSession()
        {
            if (asyncEventsDispatcher == null)
                throw LoggerContext.CaptureAndThrow(new InvalidOperationException($"Serialized reference cannot be null: {nameof(asyncEventsDispatcher)}"));
            return new WebSocketSession(asyncEventsDispatcher, LoggerContext);
        }

        private RoomsClient CreateRoomsClient() => new()
        {
            Session = _webSocketSession.Value
        };

        private RoomsManager CreateRoomsManager()
        {
            var webSocketSession = _webSocketSession.Value;
            var roomsManager = new RoomsManager(this, _roomsClient.Value, LoggerContext);
            webSocketSession.Disconnected += OnWebSocketDisconnected;
            return roomsManager;
        }
        private void OnWebSocketDisconnected(DisconnectionData _) => RoomsManager.Reset();

        private ElympicsLobbyClientState FetchState(ElympicsState state)
        {
            if (_states.TryGetValue(state, out var elympicsState))
                return elympicsState;

            var newState = StateFactory(state);
            _states.Add(state, newState);
            return newState;
        }

        private ElympicsLobbyClientState StateFactory(ElympicsState state) => state switch
        {
            ElympicsState.Disconnected => new DisconnectedState(this),
            ElympicsState.Connecting => new ConnectingState(this),
            ElympicsState.Connected => new ConnectedState(this),
            ElympicsState.Matchmaking => new MatchmakingState(this),
            ElympicsState.PlayingMatch => new PlayingMatchState(this),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        private void OnGameplayFinished() => CurrentState.FinishMatch().Forget();
        UniTask IMatchLauncher.StartMatchmaking(IRoom room) => CurrentState.StartMatchmaking(room);
        UniTask IMatchLauncher.CancelMatchmaking(IRoom room, CancellationToken ct) => CurrentState.CancelMatchmaking(room, ct);
        public void MatchFound() => CurrentState.MatchFound();

        #endregion

        #region internal methods

        internal void CheckConnectionDataOrThrow(ConnectionData data)
        {
            if (data.AuthFromCacheData is null
                && data.AuthType is null
                && data.Region == null)
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("All data parameters are null");
        }

        internal async UniTask FetchAvailableRegions()
        {
            var availableRegions = await _regionRetriever.GetAvailableRegions();
            _sessionConnectionFactory = new SessionConnectionFactory(new StandardRegionValidator(availableRegions));
            AvailableRegions = availableRegions;
        }

        internal async UniTask Authorize(ConnectionData data)
        {
            var authorizeToElympics = GetAuthStrategy(AuthData is not null);
            var authResult = await authorizeToElympics.Authorize(data);
            OnAuthenticatedWith(authResult);
        }

        internal void SignOutInternal()
        {
            var logger = LoggerContext.WithMethodName();

            if (!IsAuthenticated)
                throw new ElympicsException("User is not authenticated");

            AuthData = null;

            DisconnectFromLobby();
            logger.SetNoUser().SetNoConnection().SetNoRoom().Log("Signed out on user request.");
        }

        internal void PlayMatchInternal(MatchmakingFinishedData matchData)
        {
            MatchDataGuid = matchData;
            LoadGameplayScene();
        }

        internal async UniTask ConnectToLobby(ConnectionData data)
        {
            var lobbyConnection = GetConnectionStrategy(AuthData is not null, _webSocketSession.Value.IsConnected);
            var connectionDetails = _sessionConnectionFactory.CreateSessionConnectionDetails(_config.ElympicsWebSocketUrl, AuthData, _gameConfig, data.Region);
            await lobbyConnection.Connect(connectionDetails);
            currentRegion = connectionDetails.RegionName;
        }

        internal void SwitchState(ElympicsState newState)
        {
            _previousState = CurrentState;
            CurrentState = FetchState(newState);
            StateChanged?.Invoke(_previousState.State, CurrentState.State);
            ElympicsLogger.Log($"Switch state from {_previousState.State} to {CurrentState.State}");
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

        internal enum JoinedMatchMode
        {
            Online,
            Local,
            HalfRemoteClient,
            HalfRemoteServer,
        }

        #endregion
    }
}
