using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using JetBrains.Annotations;
namespace Elympics
{
    public partial class ElympicsLobbyClient
    {
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

        [Obsolete("Use " + nameof(ElympicsLobbyClient) + "." + nameof(Instance) + "." + nameof(IGameplaySceneMonitor) + "." + nameof(IGameplaySceneMonitor.IsCurrentlyInMatch) + " instead.")]
        public bool IsCurrentlyInMatch => GameplaySceneMonitor!.IsCurrentlyInMatch;

        #region Deprecated matchmaking

        [Obsolete("Use " + nameof(MatchDataGuid) + " instead")]
        [PublicAPI] public JoinedMatchData? MatchData { get; private set; }

        [Obsolete("Use " + nameof(RoomsManager) + " instead")]
        [PublicAPI] public IMatchmakerEvents Matchmaker => _matchmaker;

        #endregion Deprecated matchmaking

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
        [Obsolete]
        public void HasAnyUnfinishedMatch(Action<bool> onSuccess, Action<string>? onFailure = null) => _matchmaker.CheckForAnyUnfinishedMatch(new Guid(_gameConfig.GameId), _gameConfig.GameVersion, AuthData, onSuccess, e => onFailure?.Invoke(e.Message));

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
        [Obsolete]
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

        #region private

        private MatchmakerClient _matchmaker = null!;
        private bool _matchmakingInProgress;
        private async UniTask LegacyAuth(AuthType authType) => await ConnectToElympicsAsync(new ConnectionData()
        {
            AuthType = authType
        });

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

        private void HandleMatchmakingSucceeded(MatchmakingFinishedData matchData)
        {
            ElympicsLogger.Log("Matchmaking finished successfully.");
            MatchDataGuid = matchData;
            CleanUpAfterMatchmaking();
            if (ShouldLoadGameplaySceneAfterMatchmaking)
                LoadGameplayScene();
        }

        private static void HandleMatchIdReceived(Guid matchId) => ElympicsLogger.Log($"Received match ID: {matchId}.");

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

        private void CleanUpAfterMatchmaking() => _matchmakingInProgress = false;

        private static void HandleMatchmakingWarning((string Warning, Guid MatchId) args) => ElympicsLogger.LogWarning($"Matchmaking warning: {args.Warning}");

        #endregion
    }
}
