#nullable enable

using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public static class ElympicsLaunchDefaults
    {
        /// <summary>Used when <see cref="ElympicsLaunchRequest.launchTimeoutSeconds"/> is 0.</summary>
        public const int LaunchTimeoutSeconds = 60;
        /// <summary>Used when <see cref="ElympicsLaunchRequest.postMatchTimeoutSeconds"/> is 0.</summary>
        public const int PostMatchTimeoutSeconds = 300;
        /// <summary>Set <see cref="ElympicsLaunchRequest.postMatchTimeoutSeconds"/> to this to wait in <c>MatchLauncherState.Ended</c> forever.</summary>
        public const int NoPostMatchTimeout = -1;
        /// <summary>How long <c>IElympicsMatchLauncher.QuitApplication</c> lets teardown run before exiting anyway.</summary>
        public const float QuitGraceSeconds = 2f;
    }

    [PublicAPI]
    public sealed class ElympicsLaunchResult
    {
        public bool Success { get; }
        public string RequestId { get; }
        /// <summary>Set only if <see cref="Success"/> is false.</summary>
        public ElympicsLaunchErrorCode? Code { get; }
        public string Message { get; }
        public string GameId { get; }
        public string MatchId { get; }

        private ElympicsLaunchResult(bool success, string requestId, ElympicsLaunchErrorCode? code, string message, string gameId, string matchId)
        {
            Success = success;
            RequestId = requestId;
            Code = code;
            Message = message;
            GameId = gameId;
            MatchId = matchId;
        }

        internal static ElympicsLaunchResult Succeeded(string requestId, string gameId, string matchId) =>
            new(true, requestId, null, string.Empty, gameId, matchId);

        internal static ElympicsLaunchResult Failed(string requestId, ElympicsLaunchErrorCode code, string message) =>
            new(false, requestId, code, message, string.Empty, string.Empty);

        public override string ToString() => Success ? $"Launch {RequestId} succeeded (match {MatchId})" : $"Launch {RequestId} failed: {Code}: {Message}";
    }

    [PublicAPI]
    public sealed class ElympicsAbortResult
    {
        public bool Success { get; }
        public string RequestId { get; }
        /// <summary>Set only if <see cref="Success"/> is false.</summary>
        public ElympicsLaunchErrorCode? Code { get; }
        public string Message { get; }

        private ElympicsAbortResult(bool success, string requestId, ElympicsLaunchErrorCode? code, string message)
        {
            Success = success;
            RequestId = requestId;
            Code = code;
            Message = message;
        }

        internal static ElympicsAbortResult Succeeded(string requestId) => new(true, requestId, null, string.Empty);

        internal static ElympicsAbortResult Failed(string requestId, ElympicsLaunchErrorCode code, string message) =>
            new(false, requestId, code, message);

        public override string ToString() => Success ? $"Abort {RequestId} succeeded" : $"Abort {RequestId} failed: {Code}: {Message}";
    }
}
