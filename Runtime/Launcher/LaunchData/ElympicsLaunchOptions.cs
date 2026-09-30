#nullable enable

using System;
using System.Threading;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>Optional settings of one launch, shared by all match modes.</summary>
    [PublicAPI]
    public sealed class ElympicsLaunchOptions
    {
        /// <summary>Echoed in every event of this launch. Generated if null or empty.</summary>
        public string? RequestId { get; init; }
        /// <summary>
        /// How long the launch may take until the match is joined. Null means <see cref="ElympicsLaunchDefaults.LaunchTimeoutSeconds"/>,
        /// <see cref="Timeout.InfiniteTimeSpan"/> means never time out.
        /// </summary>
        public TimeSpan? LaunchTimeout { get; init; }
        /// <summary>
        /// How long to wait in <see cref="MatchLauncherState.Ended"/> for the host to return to boot. Null means
        /// <see cref="ElympicsLaunchDefaults.PostMatchTimeoutSeconds"/>, <see cref="Timeout.InfiniteTimeSpan"/> means wait forever.
        /// </summary>
        public TimeSpan? PostMatchTimeout { get; init; }
    }
}
