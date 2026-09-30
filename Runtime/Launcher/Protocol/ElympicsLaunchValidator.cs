#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    /// <summary>Launch commands of host bridges, one per match mode.</summary>
    internal enum ElympicsHostLaunchKind
    {
        Online,
        SinglePlayer,
        HalfRemoteClient,
        HalfRemoteServer,
        SnapshotReplay,
    }

    /// <summary>A validated launch.</summary>
    internal sealed class ElympicsLaunch
    {
        public string RequestId { get; }
        public ElympicsGameConfig GameConfig { get; }
        public LaunchData Data { get; }
        /// <summary>Null means never time out.</summary>
        public TimeSpan? LaunchTimeout { get; }
        /// <summary>Null means wait for the host forever.</summary>
        public TimeSpan? PostMatchTimeout { get; }

        /// <summary>The match ID reported in launcher events.</summary>
        public Guid MatchId => Data.EventMatchId;

        public ElympicsLaunch(string requestId, ElympicsGameConfig gameConfig, LaunchData data, TimeSpan? launchTimeout, TimeSpan? postMatchTimeout)
        {
            RequestId = requestId;
            GameConfig = gameConfig;
            Data = data;
            LaunchTimeout = launchTimeout;
            PostMatchTimeout = postMatchTimeout;
        }
    }

    internal static class ElympicsLaunchValidator
    {
        /// <summary>
        /// Validates a launch against the available games and the platform.
        /// Exactly one of <paramref name="launch"/> and <paramref name="failure"/> is non-null on return.
        /// </summary>
        public static bool TryValidate(
            string requestId,
            string? gameId,
            LaunchData? data,
            ElympicsLaunchOptions? options,
            IReadOnlyList<ElympicsGameConfig?> availableGames,
            bool platformCanListen,
            out ElympicsLaunch? launch,
            out ElympicsLaunchFailure? failure)
        {
            launch = null;
            failure = null;

            if (!Guid.TryParse(gameId ?? string.Empty, out var parsedGameId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"The game ID must be a GUID, got \"{gameId}\".", out failure);
            var gameConfig = availableGames.FirstOrDefault(game => game != null && Guid.TryParse(game.GameId, out var id) && id == parsedGameId);
            if (gameConfig == null)
                return Fail(ElympicsLaunchErrorCode.UnknownGame, $"Game {parsedGameId} is not in the available games of ElympicsConfig.", out failure);
            if (string.IsNullOrEmpty(gameConfig.GameplayScene))
                return Fail(ElympicsLaunchErrorCode.UnknownGame, $"Game {parsedGameId} ({gameConfig.GameName}) has no gameplay scene set.", out failure);

            if (data == null)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, "Launch data is missing.", out failure);

            if (!TryResolveTimeout(options?.LaunchTimeout, ElympicsLaunchDefaults.LaunchTimeoutSeconds, nameof(ElympicsLaunchOptions.LaunchTimeout), out var launchTimeout, out failure)
                || !TryResolveTimeout(options?.PostMatchTimeout, ElympicsLaunchDefaults.PostMatchTimeoutSeconds, nameof(ElympicsLaunchOptions.PostMatchTimeout), out var postMatchTimeout, out failure))
                return false;

            if (!data.TryValidate(gameConfig, platformCanListen, out failure))
                return false;

            launch = new ElympicsLaunch(requestId, gameConfig, data, launchTimeout, postMatchTimeout);
            return true;
        }

        /// <summary>Parses the <paramref name="optionsJson"/> argument of a launch command. Empty means all defaults.</summary>
        public static bool TryParseOptions(string? optionsJson, out ElympicsLaunchOptions? options, out ElympicsLaunchFailure? failure)
        {
            options = null;
            if (!TryFromJson<ElympicsLaunchOptionsDto>(optionsJson, "options", out var dto, out failure))
                return false;
            return dto!.TryToOptions(out options, out failure);
        }

        /// <summary>Parses the <paramref name="dataJson"/> argument of the launch command of <paramref name="kind"/>. Empty means the mode's defaults.</summary>
        public static bool TryParseData(ElympicsHostLaunchKind kind, string? dataJson, out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            return kind switch
            {
                ElympicsHostLaunchKind.Online => TryParseDto<OnlineLaunchDataDto>(dataJson, out data, out failure),
                ElympicsHostLaunchKind.SinglePlayer => TryParseDto<SinglePlayerLaunchDataDto>(dataJson, out data, out failure),
                ElympicsHostLaunchKind.HalfRemoteClient => TryParseDto<HalfRemoteClientLaunchDataDto>(dataJson, out data, out failure),
                ElympicsHostLaunchKind.HalfRemoteServer => TryParseDto<HalfRemoteServerLaunchDataDto>(dataJson, out data, out failure),
                ElympicsHostLaunchKind.SnapshotReplay => TryParseDto<SnapshotReplayLaunchDataDto>(dataJson, out data, out failure),
                _ => Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"Unknown launch command {(int)kind}.", out failure),
            };
        }

        private static bool TryParseDto<T>(string? dataJson, out LaunchData? data, out ElympicsLaunchFailure? failure) where T : class, ILaunchDataDto, new()
        {
            data = null;
            return TryFromJson<T>(dataJson, "data", out var dto, out failure) && dto!.TryToLaunchData(out data, out failure);
        }

        private static bool TryFromJson<T>(string? json, string argumentName, out T? value, out ElympicsLaunchFailure? failure) where T : class, new()
        {
            failure = null;
            value = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                value = new T();
                return true;
            }
            try
            {
                value = JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"The {argumentName} argument is not valid JSON: {e.Message}", out failure);
            }
            return value != null || Fail(ElympicsLaunchErrorCode.InvalidPayload, $"The {argumentName} argument is not a JSON object.", out failure);
        }

        private static bool TryResolveTimeout(TimeSpan? value, int defaultSeconds, string name, out TimeSpan? timeout, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            timeout = null;
            if (value == null)
            {
                timeout = TimeSpan.FromSeconds(defaultSeconds);
                return true;
            }
            if (value.Value == System.Threading.Timeout.InfiniteTimeSpan)
                return true;
            if (value.Value > TimeSpan.Zero)
            {
                timeout = value;
                return true;
            }
            return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{name} must be positive or Timeout.InfiniteTimeSpan, got {value.Value}.", out failure);
        }

        private static bool Fail(ElympicsLaunchErrorCode code, string message, out ElympicsLaunchFailure? failure)
        {
            failure = new ElympicsLaunchFailure(code, message);
            return false;
        }
    }
}
