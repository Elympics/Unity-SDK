#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
    internal static class LaunchDataFromParameters
    {
        public const string HalfRemoteClient = "halfRemoteClient";
        public const string HalfRemoteServer = "halfRemoteServer";
        public const string SinglePlayer = "singlePlayer";
        public const string SnapshotReplay = "snapshotReplay";

        public static readonly IReadOnlyList<string> All = new[] { HalfRemoteClient, HalfRemoteServer, SinglePlayer, SnapshotReplay };

        /// <returns>The launch mode matching <paramref name="mode"/> case-insensitively, or null.</returns>
        public static string? Normalize(string? mode) => All.FirstOrDefault(known => string.Equals(known, mode, StringComparison.OrdinalIgnoreCase));

        public static bool IsSet => !string.IsNullOrEmpty(ApplicationParameters.Parameters.LaunchMode.GetValue());

        public static bool TryCreate(string? mode, string? replayPath, ElympicsGameConfig gameConfig, out LaunchData? data, out string error)
        {
            error = string.Empty;
            // The half-remote endpoints and player index reach the settings through the ElympicsGameConfig getters, which read their parameters.
            data = Normalize(mode) switch
            {
                HalfRemoteClient => new HalfRemoteClientLaunchData(),
                HalfRemoteServer => new HalfRemoteServerLaunchData(),
                SinglePlayer => new SinglePlayerLaunchData(),
                SnapshotReplay => new SnapshotReplayLaunchData(string.IsNullOrEmpty(replayPath) ? gameConfig.SnapshotFilePath : replayPath!),
                _ => null,
            };
            if (data != null)
                return true;
            error = $"Unknown launch mode \"{mode}\". Use {string.Join(", ", All)}.";
            return false;
        }
    }
}
