using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Communication.Lobby.Models.ToLobby;
using Cysharp.Threading.Tasks;
using Elympics.AssemblyCommunicator;
using Elympics.AssemblyCommunicator.Events;
using Elympics.Communication.Authentication.Models;
using Elympics.Communication.Lobby.Models.FromLobby;
using Elympics.Communication.Lobby.Models.ToLobby;
using Elympics.Communication.Mappers;
using Elympics.ElympicsSystems;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using Elympics.SnapshotAnalysis.Retrievers;
using Elympics.Util;
using JetBrains.Annotations;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.SceneManagement;
using MatchmakingState = Elympics.ElympicsSystems.Internal.MatchmakingState;

#nullable enable

#pragma warning disable CS0618

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.ElympicsLobbyClient)]
    [RequireComponent(typeof(AsyncEventsDispatcher))]
    public partial class ElympicsLobbyClient : MonoBehaviour, IMatchLauncher, IAuthManager, ILobby, IWebSocketSessionController
    {
        private const string NoGameModeName = "None";

        private static readonly MatchmakingFinishedData SinglePlayerMatchmakingFinishedData = new(Guid.Empty,
            new MatchDetails(
                new[] { Guid.Empty },
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<byte>(),
                Array.Empty<float>()),
            string.Empty,
            string.Empty);

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

        [Obsolete("Use instead" + nameof(ElympicsConnectionEstablished))]
        public event Action<AuthData>? AuthenticationSucceeded;

        [Obsolete]
        public event Action<string>? AuthenticationFailed;

        public event Action<ElympicsConnectionData>? ElympicsConnectionEstablished;
        public event Action<ElympicsConnectionLostData>? ElympicsConnectionLost;

        private IAuthClient _auth = null!;
        public AuthData? AuthData { get; internal set; }

        [PublicAPI]
        public ElympicsUser? ElympicsUser { get; internal set; }

        public Guid? UserGuid => AuthData?.UserId;
        public bool IsAuthenticated => AuthData != null;
        private string? _clientSecret;
        private SessionConnectionFactory _sessionConnectionFactory = null!;

        #endregion Authentication

        [PublicAPI]
        public IGameplaySceneMonitor? GameplaySceneMonitor { get; private set; }

        [PublicAPI]
        public IWebSocketSession WebSocketSession => _webSocketSession.IsValueCreated ? _webSocketSession.Value
            : throw new InvalidOperationException($"The instance of {nameof(ElympicsLobbyClient)} has not been initialized correctly.");

        #region Rooms

        [PublicAPI]
        public IRoomsManager RoomsManager => _roomsManager.IsValueCreated ? _roomsManager.Value
            : throw new InvalidOperationException($"The instance of {nameof(ElympicsLobbyClient)} has not been initialized correctly.");

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

        [Tooltip("Default starting value. The value can be changed at runtime using "
            + nameof(ElympicsLobbyClient)
            + "."
            + nameof(Instance)
            + "."
            + nameof(ShouldLoadGameplaySceneAfterMatchmaking)
            + " property.")]
        [SerializeField]
        private bool shouldLoadGameplaySceneAfterMatchmaking = true;

        public bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        #endregion Matchmaking

        [SerializeField] private string currentRegion = string.Empty;

        [PublicAPI]
        public string CurrentRegion => currentRegion;

        [PublicAPI]
        public IReadOnlyCollection<string>? AvailableRegions { get; private set; }

        [PublicAPI]
        public IReadOnlyCollection<CoinInfo>? AvailableCoins { get; private set; }

        private IAvailableRegionRetriever _regionRetriever = null!;

        private ElympicsConfig _config = null!;
        private ElympicsGameConfig _gameConfig = null!;

        private static ElympicsLoggerContext loggerContext;
        SnapshotAnalysisRetriever? ILobby.SnapshotAnalysisRetriever => _snapshotAnalysisRetriever;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private DefaultSnapshotAnalysisRetriever? _snapshotAnalysisRetriever;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
                throw loggerContext.CaptureAndThrow(new InvalidOperationException($"Serialized field {nameof(asyncEventsDispatcher)} cannot be null."));

            LobbyRegister.ElympicsLobby = this;
            _config = ElympicsConfig.Load() ?? throw loggerContext.CaptureAndThrow(new InvalidOperationException($"No {nameof(ElympicsConfig)} instance found."));

            _config.CurrentGameSwitched += UniTask.Action(async () => await UpdateGameConfig());
            _gameConfig = _config.GetCurrentGameConfig()
                ?? throw loggerContext.CaptureAndThrow(new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly."));
            loggerContext = new ElympicsLoggerContext(ElympicsLogger.SessionId)
            {
                Context = nameof(ElympicsLobbyClient),
            }
            .SetElympicsContext(ElympicsConfig.SdkVersion, _gameConfig.gameId)
            .SetGameMode(NoGameModeName)
            .WithApp(ElympicsLoggerContext.ElympicsContextApp);
            _regionRetriever = new DefaultRegionRetriever();

            var awakeLogger = loggerContext.WithMethodName();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _clientSecret = GetOrCreateClientSecret();

            awakeLogger.Log(
                $"Initializing Elympics menu scene... {Environment.NewLine} Available games: {Environment.NewLine} {string.Join($"{Environment.NewLine}", _config.AvailableGames.Select(game => $"{game.GameName} (ID: {game.GameId}), version {game.GameVersion}"))}");

            if (string.IsNullOrEmpty(_config.ElympicsLobbyEndpoint))
                throw awakeLogger.CaptureAndThrow(new ArgumentException($"Elympics authentication endpoint not set. Finish configuration using [{ElympicsEditorMenuPaths.SETUP_MENU_PATH}].",
                    nameof(_config.ElympicsAuthEndpoint)));

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
                ConnectToElympicsAsync(new ConnectionData
                {
                    AuthType = authenticateOnAwakeWith,
                }).Forget();
        }

        #region Public API

        [PublicAPI]
        public async UniTask ConnectToElympicsAsync(ConnectionData data)
        {
            try
            {
                await CurrentState.Connect(data);
            }
            catch (Exception e)
            {
                AuthData = null;
                throw loggerContext.CaptureAndThrow(e);
            }
        }

        [PublicAPI]
        internal async UniTask WatchReplay(string matchId)
        {
            _snapshotAnalysisRetriever = new DefaultSnapshotAnalysisRetriever(_config.ElympicsReplaySource, this);
            await _snapshotAnalysisRetriever.RetrieveSnapshotReplay(matchId);
            WatchReplay();
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
        public void PlaySinglePlayer()
        {
            LogSettingUpGame("Single Player");
            PlayMatch(SinglePlayerMatchmakingFinishedData);
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

        #endregion

        #region IMatchLauncher

        public void PlayMatch(MatchmakingFinishedData matchData)
        {
            loggerContext.Log($"Play match.");
            CurrentState.PlayMatch(matchData).Forget();
        }

        internal void WatchReplay() => CurrentState.WatchReplay();

        #endregion

        #region private methods

        private void OnAuthenticatedWith(Result<AuthData, string> result)
        {
            var logger = loggerContext.WithMethodName();
            string? eventName = null;
            try
            {
                if (result.IsSuccess)
                {
                    if (result.Value != null)
                    {
                        AuthData = result.Value;
                        logger.SetUserId(result.Value.UserId.ToString()).SetAuthType(result.Value.AuthType).Log("Authentication completed.");
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
                AuthenticatedGuid?.Invoke(
                    result.IsSuccess ? Result<AuthenticationData, string>.Success(new AuthenticationData(result.Value)) : Result<AuthenticationData, string>.Failure(result.Error));
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
            _roomsManager.Value.Reset();
        }
        private async UniTask UpdateGameConfig()
        {
            _gameConfig = _config.GetCurrentGameConfig()
                ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found. Make sure {nameof(ElympicsConfig)} is set up correctly.");
            ElympicsLogger.Log($"Current game has been changed to {_gameConfig.GameName} (ID: {_gameConfig.GameId}).");
            GameplaySceneMonitor!.GameConfigChanged(_gameConfig.gameplayScene);

            try
            {
                if (AuthData is not null)
                    await CurrentState.Reconnect(new ConnectionData()
                    {
                        Region = new RegionData(currentRegion),
                        AuthFromCacheData = new CachedAuthData
                        {
                            CachedData = AuthData,
                            AutoRetryIfExpired = false
                        }
                    });
            }
            catch (Exception ex)
            {
                _ = ElympicsLogger.LogException(ex);
            }
        }
        private void LogSettingUpGame(string gameModeName) =>
            loggerContext.Log($"Setting up {gameModeName} mode for {_gameConfig.GameName} (ID: {_gameConfig.GameId}), version {_gameConfig.GameVersion}");

        private AuthorizationStrategy GetAuthStrategy(bool isAuthorized) => isAuthorized switch
        {
            true => new AuthorizedStrategy(AuthData, _auth, _clientSecret, ethSigner, _telegramSigner),
            false => new UnauthorizedStrategy(_auth, _clientSecret, ethSigner, _telegramSigner),
        };

        private ConnectionStrategy GetConnectionStrategy(bool isAuthenticated, bool isConnected) => (isAuthenticated, isConnected) switch
        {
            (true, true) => new AuthorizedConnectedSocketConnectionStrategy(_webSocketSession.Value, _webSocketSession.Value.ConnectionDetails!.Value, loggerContext),
            (true, false) => new AuthorizedNotConnectedStrategy(_webSocketSession.Value, loggerContext),
            (false, _) => new UnauthorizedSocketConnectionStrategy(_webSocketSession.Value, loggerContext),
        };


        private void SetUpMatch(JoinedMatchMode mode) => MatchMode = mode;

        private void LoadGameplayScene() => SceneManager.LoadScene(_gameConfig.GameplayScene);

        private const string ClientSecretPlayerPrefsKeyBase = "Elympics/AuthToken";

        private static string ClientSecretPlayerPrefsKey =>
            ElympicsClonesManager.IsClone() ? $"{ClientSecretPlayerPrefsKeyBase}_clone_{ElympicsClonesManager.GetCloneNumber()}" : ClientSecretPlayerPrefsKeyBase;

        private static string CreateNewClientSecret() => Guid.NewGuid().ToString();

        private void OnDestroy()
        {
            if (_webSocketSession.IsValueCreated)
                _webSocketSession.Value.Dispose();
            GameplaySceneMonitor?.Dispose();
        }

        private WebSocketSession CreateWebSocketSession()
        {
            if (asyncEventsDispatcher == null)
                throw loggerContext.CaptureAndThrow(new InvalidOperationException($"Serialized reference cannot be null: {nameof(asyncEventsDispatcher)}"));
            return new WebSocketSession(this, asyncEventsDispatcher, loggerContext);
        }

        private RoomsClient CreateRoomsClient() => new(loggerContext)
        {
            Session = _webSocketSession.Value,
        };

        private RoomsManager CreateRoomsManager()
        {
            var roomsManager = new RoomsManager(this, _roomsClient.Value, loggerContext);
            return roomsManager;
        }

        public async UniTask ReconnectIfPossible(DisconnectionData reason)
        {
            RoomsManager.Reset();
            if (reason.Reason != DisconnectionReason.Timeout)
            {
                await CurrentState.Disconnect();
                ElympicsConnectionLost?.Invoke(new ElympicsConnectionLostData
                {
                    DisconnectionData = reason
                });
                return;
            }

            var reconnectionData = new ConnectionData
            {
                AuthType = null,
                Region = new RegionData
                {
                    Name = CurrentRegion,
                },
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = AuthData,
                }
            };
            await CurrentState.Reconnect(reconnectionData);
            if (CurrentState.State == ElympicsState.Disconnected)
                ElympicsConnectionLost?.Invoke(new ElympicsConnectionLostData
                {
                    DisconnectionData = reason.Reason == DisconnectionReason.Reconnection ? new DisconnectionData(DisconnectionReason.Timeout) : reason,
                });
            else
                ElympicsConnectionEstablished?.Invoke(new ElympicsConnectionData
                {
                    AuthData = AuthData,
                    AutoReconnected = true,
                });
        }

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
            ElympicsState.WatchReplay => new WatchReplayState(this),
            ElympicsState.Reconnecting => new ReconnectingState(this, loggerContext),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        private void OnGameplayFinished()
        {
            CurrentState.FinishMatch().ContinueWith(UpdateLoggerContext).Forget(HandleException);

            void UpdateLoggerContext() => loggerContext.SetGameMode(NoGameModeName);

            void HandleException(Exception e)
            {
                loggerContext.Exception(e);
                UpdateLoggerContext();
            }
        }

        UniTask IMatchLauncher.StartMatchmaking(IRoom room) => CurrentState.StartMatchmaking(room);
        UniTask IMatchLauncher.CancelMatchmaking(IRoom room, CancellationToken ct) => CurrentState.CancelMatchmaking(room, ct);
        public void MatchmakingCompleted() => CurrentState.MatchFound();

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
            var logger = loggerContext.WithMethodName();
            ClearAuthData();
            DisconnectFromLobby();
            logger.Log("User sign out.");
            _ = logger.SetNoUser().SetNoConnection().SetNoRoom();
        }

        internal void ClearAuthData()
        {
            AuthData = null;
            ElympicsUser = null;
        }

        internal void PlayMatchInternal(MatchmakingFinishedData matchData)
        {
            MatchDataGuid = matchData;

            var isSinglePlayer = string.IsNullOrEmpty(matchData.WebServerAddress) && string.IsNullOrEmpty(matchData.TcpUdpServerAddress);

            SetUpMatch(isSinglePlayer ? JoinedMatchMode.SinglePlayer : JoinedMatchMode.Online);
            LoadGameplayScene();
        }
        internal void FinishMatchInternal() => _snapshotAnalysisRetriever = null;

        internal void WatchReplayInternal()
        {
            SetUpMatch(JoinedMatchMode.SnapshotReplay);
            LoadGameplayScene();
        }

        /// <summary>
        /// Connects to lobby services, performing a handshake for exchanging client-side and server-side game details.
        /// </summary>
        /// <param name="data">Authentication type and region data.</param>
        /// <returns>Server-side game details (only if <paramref name="data"/>, <see cref="AuthData"/>, <see cref="_config"/>, or <see cref="_gameConfig"/> changed since last call).</returns>
        internal async UniTask<GameDataResponseDto?> ConnectToLobby(ConnectionData data)
        {
            var lobbyConnection = GetConnectionStrategy(AuthData is not null, _webSocketSession.Value.IsConnected);
            var connectionDetails = _sessionConnectionFactory.CreateSessionConnectionDetails(_config.ElympicsWebSocketUrl, AuthData, _gameConfig, data.Region);
            var gameData = await lobbyConnection.Connect(connectionDetails);
            currentRegion = connectionDetails.RegionName;
            return gameData;
        }

        [PublicAPI]
        public async UniTask<TournamentFeeInfo?> GetRollTournamentsFee(TournamentFeeRequestInfo[] requestData, CancellationToken ct = default)
        {
            if (requestData.Length == 0)
                return null;

            var response = await GetRollTournamentsFeeInternal(requestData, ct);

            if (response == null)
                return null;

            var fees = new FeeInfo[response.Rollings.Count];

            for (var i = 0; i < fees.Length; i++)
            {
                var fee = response.Rollings[i];
                var coinId = requestData[i].CoinInfo.Id;
                var prize = requestData[i].Prize;
                var numberOfPlayers = requestData[i].PlayersCount;
                fees[i] = new FeeInfo
                {
                    EntryFee = RawCoinConverter.FromRaw(fee.EntryFee,
                        FetchDecimalForCoin(coinId) ?? throw new Exception($"Coin with ID {coinId} was not found when processing rolling tournament fees.")),
                    Error = fee.Error,
                    EntryFeeRaw = fee.EntryFee
                };

                RollingTournamentBetConfigIDs.AddOrUpdate(coinId, prize, numberOfPlayers, fee.RollingTournamentBetConfigId);
            }

            return new TournamentFeeInfo { Fees = fees };
        }

        internal async Task<RollingsResponseDto?> GetRollTournamentsFeeInternal(TournamentFeeRequestInfo[] requestData, CancellationToken ct)
        {
            var config = _config.GetCurrentGameConfig() ?? throw new InvalidOperationException("No game config available");
            var request = new RequestRollingsDto(
                GameId: Guid.Parse(config.GameId),
                VersionId: config.GameVersion,
                Rollings: requestData.Select(x => new RollingRequestDto(
                        CoinId: x.CoinInfo.Id,
                        Prize: RawCoinConverter.ToRaw(x.Prize,
                        decimals: x.CoinInfo.Currency.Decimals),
                        PlayersCount: (uint)x.PlayersCount,
                        PrizeDistribution: x.PrizeDistribution ?? Array.Empty<decimal>()))
                    .ToList());

            return await _webSocketSession.Value.SendRequest<RollingsResponseDto>(request, ct);
        }

        internal async UniTask InitializeBasedOnGameData(GameDataResponseDto gameDataResponse)
        {
            var coins = new List<CoinInfo>(gameDataResponse.CoinData.Count);

            foreach (var coin in gameDataResponse.CoinData)
                coins.Add(await coin.Map().ToCoinInfo(loggerContext));
            AvailableCoins = coins;
            await _roomsManager.Value.CheckJoinedRoomStatus(gameDataResponse);
        }

        internal async UniTask GetElympicsUserData()
        {
            loggerContext.Log("Start fetching user data...");
            var response = await _webSocketSession.Value.SendRequest<ShowAuthResponseDto>(new ShowAuthDto());
            ElympicsUser = new ElympicsUser
            {
                UserId = response.UserId,
                Nickname = response.Nickname,
                AvatarUrl = response.AvatarUrl,
            };
            loggerContext.SetNickname(response.Nickname).Log($"User data retrieved.");

        }

        internal void SwitchState(ElympicsState newState)
        {
            _previousState = CurrentState;
            CurrentState = FetchState(newState);
            StateChanged?.Invoke(_previousState.State, CurrentState.State);
            CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsStateChanged
            {
                PreviousState = _previousState.State,
                NewState = CurrentState.State
            });
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
            ElympicsLogger.Log($"Starting matchmaking process for user: {userId}, region: {regionName}, queue: {queueName}\nSupplied matchmaker data: {serializedMmData}\n"
                + $"Supplied game engine data: {serializedGeData}");
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
            SinglePlayer,
            HalfRemoteClient,
            HalfRemoteServer,
            SnapshotReplay,
        }

        #endregion

        internal int? FetchDecimalForCoin(Guid coinId)
        {
            if (AvailableCoins == null)
                return null;

            foreach (var coinInfo in AvailableCoins)
                if (coinInfo.Id == coinId)
                    return coinInfo.Currency.Decimals;

            return null;
        }
        internal void OnSuccessfullyConnectedToElympics(bool reconnected) => ElympicsConnectionEstablished?.Invoke(new ElympicsConnectionData()
        {
            AuthData = AuthData,
            AutoReconnected = reconnected,
        });
    }
}
