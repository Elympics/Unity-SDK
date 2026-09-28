#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal enum ElympicsLaunchSource
    {
        /// <summary>A C# caller in the same process, e.g. PlayPad or a game's menu.</summary>
        InProcess,
        /// <summary>An out-of-process host, through a host bridge.</summary>
        Wire,
    }

    /// <summary>A validated <see cref="ElympicsLaunchRequest"/>.</summary>
    internal sealed class ElympicsLaunch
    {
        public string RequestId { get; }
        public ElympicsGameConfig GameConfig { get; }
        public JoinedMatchMode MatchMode { get; }
        public Guid UserId { get; }
        /// <summary>Null for modes configured entirely from <see cref="ElympicsGameConfig"/>.</summary>
        public MatchmakingFinishedData? MatchData { get; }
        public SnapshotAnalysisRetriever? SnapshotRetriever { get; }
        public TimeSpan LaunchTimeout { get; }
        /// <summary>Null means wait for the host forever.</summary>
        public TimeSpan? PostMatchTimeout { get; }

        public ElympicsLaunch(string requestId, ElympicsGameConfig gameConfig, JoinedMatchMode matchMode, Guid userId,
            MatchmakingFinishedData? matchData, SnapshotAnalysisRetriever? snapshotRetriever, TimeSpan launchTimeout, TimeSpan? postMatchTimeout)
        {
            RequestId = requestId;
            GameConfig = gameConfig;
            MatchMode = matchMode;
            UserId = userId;
            MatchData = matchData;
            SnapshotRetriever = snapshotRetriever;
            LaunchTimeout = launchTimeout;
            PostMatchTimeout = postMatchTimeout;
        }
    }

    internal static class ElympicsLaunchRequestParser
    {
        internal const string CannotListenMessage = "platform cannot open a listening socket";

        /// <summary>
        /// Validates <paramref name="request"/> against the available games and the platform.
        /// Exactly one of <paramref name="launch"/> and <paramref name="failure"/> is non-null on return.
        /// </summary>
        public static bool TryParse(
            ElympicsLaunchRequest? request,
            IReadOnlyList<ElympicsGameConfig?> availableGames,
            ElympicsLaunchSource source,
            bool platformCanListen,
            out ElympicsLaunch? launch,
            out ElympicsLaunchFailure? failure)
        {
            launch = null;
            failure = null;

            if (request == null)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, "Launch request is missing.", out failure);

            if (string.IsNullOrWhiteSpace(request.protocolVersion))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.protocolVersion)} is required.", out failure);
            if (!ElympicsHostProtocol.IsCompatible(request.protocolVersion))
                return Fail(ElympicsLaunchErrorCode.ProtocolMismatch,
                    $"Protocol version {request.protocolVersion} is not compatible with {ElympicsHostProtocol.Version}.", out failure);

            if (!TryParseGuid(request.gameId, out var gameId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.gameId)} must be a GUID, got \"{request.gameId}\".", out failure);
            var gameConfig = availableGames.FirstOrDefault(game => game != null && TryParseGuid(game.GameId, out var id) && id == gameId);
            if (gameConfig == null)
                return Fail(ElympicsLaunchErrorCode.UnknownGame, $"Game {gameId} is not in the available games of ElympicsConfig.", out failure);
            if (string.IsNullOrEmpty(gameConfig.GameplayScene))
                return Fail(ElympicsLaunchErrorCode.UnknownGame, $"Game {gameId} ({gameConfig.GameName}) has no gameplay scene set.", out failure);

            if (!TryParseMatchMode(request.matchMode, out var matchMode))
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"Unknown match mode \"{request.matchMode}\".", out failure);

            if (request.launchTimeoutSeconds < 0)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.launchTimeoutSeconds)} must not be negative.", out failure);
            if (request.postMatchTimeoutSeconds < ElympicsLaunchDefaults.NoPostMatchTimeout)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload,
                    $"{nameof(request.postMatchTimeoutSeconds)} must be positive, 0 (default) or {ElympicsLaunchDefaults.NoPostMatchTimeout} (never).", out failure);
            var launchTimeout = TimeSpan.FromSeconds(request.launchTimeoutSeconds == 0 ? ElympicsLaunchDefaults.LaunchTimeoutSeconds : request.launchTimeoutSeconds);
            TimeSpan? postMatchTimeout = request.postMatchTimeoutSeconds switch
            {
                ElympicsLaunchDefaults.NoPostMatchTimeout => null,
                0 => TimeSpan.FromSeconds(ElympicsLaunchDefaults.PostMatchTimeoutSeconds),
                var seconds => TimeSpan.FromSeconds(seconds),
            };

            var requestId = string.IsNullOrEmpty(request.requestId) ? Guid.NewGuid().ToString("N") : request.requestId;

            switch (matchMode)
            {
                case JoinedMatchMode.Online:
                    if (!TryParseOnline(request, out var userId, out var onlineData, out failure))
                        return false;
                    launch = new ElympicsLaunch(requestId, gameConfig, matchMode, userId, onlineData, null, launchTimeout, postMatchTimeout);
                    return true;

                case JoinedMatchMode.SinglePlayer:
                    if (!TryParseSinglePlayer(request, out var singlePlayerUserId, out var singlePlayerData, out failure))
                        return false;
                    launch = new ElympicsLaunch(requestId, gameConfig, matchMode, singlePlayerUserId, singlePlayerData, null, launchTimeout, postMatchTimeout);
                    return true;

                case JoinedMatchMode.HalfRemoteServer when !platformCanListen:
                    return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{matchMode} is not supported: {CannotListenMessage}.", out failure);

                case JoinedMatchMode.SnapshotReplay when source == ElympicsLaunchSource.Wire:
                    return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{matchMode} needs an in-process snapshot source and cannot be launched over the wire.", out failure);

                case JoinedMatchMode.SnapshotReplay when request.SnapshotRetriever == null:
                    return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{matchMode} requires a snapshot retriever to be set on the request.", out failure);

                case JoinedMatchMode.HalfRemoteClient:
                case JoinedMatchMode.HalfRemoteServer:
                case JoinedMatchMode.SnapshotReplay:
                    var carried = CarriedMatchFields(request);
                    if (carried.Count > 0)
                        return Fail(ElympicsLaunchErrorCode.InvalidPayload,
                            $"{matchMode} is configured from ElympicsGameConfig and does not read {string.Join(", ", carried)}. Remove them from the request.", out failure);
                    launch = new ElympicsLaunch(requestId, gameConfig, matchMode, Guid.Empty, null, request.SnapshotRetriever, launchTimeout, postMatchTimeout);
                    return true;

                default:
                    return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"Unsupported match mode {matchMode}.", out failure);
            }
        }

        private static bool TryParseOnline(ElympicsLaunchRequest request, out Guid userId, out MatchmakingFinishedData? matchData, out ElympicsLaunchFailure? failure)
        {
            matchData = null;
            if (!TryParseGuid(request.userId, out userId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.userId)} must be a GUID, got \"{request.userId}\".", out failure);
            if (!TryParseGuid(request.matchId, out var matchId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.matchId)} must be a GUID, got \"{request.matchId}\".", out failure);
            if (string.IsNullOrEmpty(request.userSecret))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.userSecret)} is required in Online mode.", out failure);
            if (string.IsNullOrEmpty(request.webServerAddress) && string.IsNullOrEmpty(request.tcpUdpServerAddress))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload,
                    $"Either {nameof(request.webServerAddress)} or {nameof(request.tcpUdpServerAddress)} is required in Online mode.", out failure);
            if (!TryParseMatchedPlayers(request.matchedPlayers, out var players, out failure))
                return false;
            if (players.Length == 0)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.matchedPlayers)} is required in Online mode.", out failure);
            if (!players.Contains(userId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.userId)} {userId} is not one of {nameof(request.matchedPlayers)}.", out failure);
            if (!TryParseBase64(request.gameEngineData, out var gameEngineData, out failure))
                return false;

            matchData = new MatchmakingFinishedData(matchId, request.userSecret, request.queueName ?? "", request.regionName ?? "",
                gameEngineData, request.matchmakerData ?? Array.Empty<float>(), request.tcpUdpServerAddress ?? "", request.webServerAddress ?? "", players);
            return true;
        }

        private static bool TryParseSinglePlayer(ElympicsLaunchRequest request, out Guid userId, out MatchmakingFinishedData? matchData, out ElympicsLaunchFailure? failure)
        {
            userId = Guid.Empty;
            matchData = null;
            if (!string.IsNullOrEmpty(request.userId) && !TryParseGuid(request.userId, out userId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.userId)} must be a GUID, got \"{request.userId}\".", out failure);
            var matchId = Guid.Empty;
            if (!string.IsNullOrEmpty(request.matchId) && !TryParseGuid(request.matchId, out matchId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(request.matchId)} must be a GUID, got \"{request.matchId}\".", out failure);
            if (!TryParseMatchedPlayers(request.matchedPlayers, out var players, out failure))
                return false;
            if (!TryParseBase64(request.gameEngineData, out var gameEngineData, out failure))
                return false;
            if (players.Length == 0)
                players = new[] { userId };
            else if (userId == Guid.Empty)
                userId = players[0];

            matchData = new MatchmakingFinishedData(matchId, request.userSecret ?? "", request.queueName ?? "", request.regionName ?? "",
                gameEngineData, request.matchmakerData ?? Array.Empty<float>(), request.tcpUdpServerAddress ?? "", request.webServerAddress ?? "", players);
            return true;
        }

        private static List<string> CarriedMatchFields(ElympicsLaunchRequest request)
        {
            var carried = new List<string>();
            AddIfSet(request.userId, nameof(request.userId));
            AddIfSet(request.matchId, nameof(request.matchId));
            AddIfSet(request.userSecret, nameof(request.userSecret));
            AddIfSet(request.queueName, nameof(request.queueName));
            AddIfSet(request.regionName, nameof(request.regionName));
            AddIfSet(request.tcpUdpServerAddress, nameof(request.tcpUdpServerAddress));
            AddIfSet(request.webServerAddress, nameof(request.webServerAddress));
            AddIfSet(request.gameEngineData, nameof(request.gameEngineData));
            if (request.matchmakerData is { Length: > 0 })
                carried.Add(nameof(request.matchmakerData));
            if (request.matchedPlayers is { Length: > 0 })
                carried.Add(nameof(request.matchedPlayers));
            return carried;

            void AddIfSet(string? value, string name)
            {
                if (!string.IsNullOrEmpty(value))
                    carried.Add(name);
            }
        }

        internal static bool TryParseMatchMode(string? name, out JoinedMatchMode matchMode)
        {
            matchMode = default;
            // By name only: Enum.TryParse would also accept ordinals such as "1".
            if (string.IsNullOrEmpty(name) || !char.IsLetter(name![0]))
                return false;
            return Enum.TryParse(name, false, out matchMode) && Enum.IsDefined(typeof(JoinedMatchMode), matchMode);
        }

        private static bool TryParseGuid(string? value, out Guid guid)
        {
            guid = Guid.Empty;
            return !string.IsNullOrEmpty(value) && Guid.TryParse(value, out guid);
        }

        private static bool TryParseMatchedPlayers(string[]? values, out Guid[] players, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            values ??= Array.Empty<string>();
            players = new Guid[values.Length];
            for (var i = 0; i < values.Length; i++)
                if (!TryParseGuid(values[i], out players[i]))
                    return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"matchedPlayers[{i}] must be a GUID, got \"{values[i]}\".", out failure);
            if (players.Distinct().Count() != players.Length)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, "matchedPlayers must not contain duplicates.", out failure);
            return true;
        }

        private static bool TryParseBase64(string? value, out byte[] bytes, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            bytes = Array.Empty<byte>();
            if (string.IsNullOrEmpty(value))
                return true;
            try
            {
                bytes = Convert.FromBase64String(value);
                return true;
            }
            catch (FormatException)
            {
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, "gameEngineData must be base64.", out failure);
            }
        }

        private static bool Fail(ElympicsLaunchErrorCode code, string message, out ElympicsLaunchFailure? failure)
        {
            failure = new ElympicsLaunchFailure(code, message);
            return false;
        }
    }
}
