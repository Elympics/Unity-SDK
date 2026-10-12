#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    internal enum ElympicsHostLaunchKind
    {
        Online,
        SinglePlayer,
        HalfRemoteClient,
        HalfRemoteServer,
        SnapshotReplay,
    }

    internal sealed class ElympicsLaunch
    {
        public string RequestId { get; }
        public ElympicsGameConfig GameConfig { get; }
        public LaunchData Data { get; }
        /// <summary>Null means never time out.</summary>
        public TimeSpan? LaunchTimeout { get; }
        /// <summary>Null means wait for the host forever.</summary>
        public TimeSpan? PostMatchTimeout { get; }

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
        public static bool TryValidate(
            string requestId,
            string? gameId,
            LaunchData? data,
            ElympicsLaunchOptions? options,
            IReadOnlyList<ElympicsGameConfig?> availableGames,
            ElympicsLaunchPlatform platform,
            out ElympicsLaunch? launch,
            out ElympicsLaunchFailure? failure)
        {
            launch = null;
            failure = null;

            if (!ElympicsLaunchDtoParsing.TryParseGuid(gameId, nameof(gameId), true, out var parsedGameIdOrNull, out failure))
                return false;
            var parsedGameId = parsedGameIdOrNull!.Value;
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

            if (!data.TryValidate(gameConfig, platform, out failure))
                return false;

            launch = new ElympicsLaunch(requestId, gameConfig, data, launchTimeout, postMatchTimeout);
            return true;
        }

        public static bool TryParseOptions(string? optionsJson, out ElympicsLaunchOptions? options, out ElympicsLaunchFailure? failure)
        {
            options = null;
            if (!TryFromJson(optionsJson, "options", new ElympicsLaunchOptionsDto(), out var dto, out failure))
                return false;
            return dto!.TryToOptions(out options, out failure);
        }

        /// <summary>The request ID of options that failed validation, so the failure can still be matched to the request. Empty for malformed JSON.</summary>
        public static string ReadRequestId(string? optionsJson) =>
            TryFromJson(optionsJson, "options", new ElympicsLaunchOptionsDto(), out var dto, out _) ? dto!.requestId : string.Empty;

        public static bool TryParseData(ElympicsHostLaunchKind kind, string? dataJson, out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            if (!Enum.IsDefined(typeof(ElympicsHostLaunchKind), kind))
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"Unknown launch command {(int)kind}.", out failure);
            return TryFromJson(dataJson, "data", ElympicsLaunchDtos.Create(kind), out var dto, out failure) && dto!.TryToLaunchData(out data, out failure);
        }

        /// <param name="empty">Returned for empty JSON. Its runtime type is the type parsed.</param>
        private static bool TryFromJson<T>(string? json, string argumentName, T empty, out T? value, out ElympicsLaunchFailure? failure) where T : class
        {
            failure = null;
            value = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                value = empty;
                return true;
            }
            try
            {
                value = (T?)JsonUtility.FromJson(json, empty.GetType());
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
