#nullable enable

using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// Events are delivered as <c>(type, requestId, payloadJson)</c>, with <c>type</c> one of <see cref="Events"/>
    /// and <c>payloadJson</c> the JSON of the matching payload class (e.g. <see cref="ElympicsLaunchFailure"/>), or empty.
    /// </summary>
    [PublicAPI]
    public static class ElympicsHostProtocol
    {
        /// <summary>Version of the wire format (command arguments and event payloads), reported in the <c>ready</c> event.</summary>
        public const string Version = "3.0.0";

        public static class Events
        {
            public const string Ready = "ready";
            public const string LaunchAccepted = "launchAccepted";
            public const string LaunchSucceeded = "launchSucceeded";
            public const string LaunchFailed = "launchFailed";
            public const string ReturnToBootAccepted = "returnToBootAccepted";
            public const string ReturnToBootFailed = "returnToBootFailed";
            public const string MatchEnded = "matchEnded";
            public const string Disconnected = "disconnected";
            public const string MatchEndTimedOut = "matchEndTimedOut";
            public const string Idle = "idle";
            public const string Quitting = "quitting";
            public const string Pong = "pong";
        }
    }
}
