#nullable enable

using System;

namespace Elympics
{
    internal static class LaunchDataFromParameters
    {
        public const string HalfRemoteClient = "halfRemoteClient";
        public const string HalfRemoteServer = "halfRemoteServer";
        public const string SinglePlayer = "singlePlayer";
        public const string SnapshotReplay = "snapshotReplay";

        public static bool IsSet => !string.IsNullOrEmpty(ApplicationParameters.Parameters.LaunchMode.GetValue());

        public static bool TryCreate(string? mode, string? replayPath, ElympicsGameConfig gameConfig, out LaunchData? data, out string error)
        {
            error = string.Empty;
            // The half-remote endpoints and player index reach the settings through the ElympicsGameConfig getters, which read their parameters.
            data = mode switch
            {
                _ when Is(mode, HalfRemoteClient) => new HalfRemoteClientLaunchData(),
                _ when Is(mode, HalfRemoteServer) => new HalfRemoteServerLaunchData(),
                _ when Is(mode, SinglePlayer) => new SinglePlayerLaunchData(),
                _ when Is(mode, SnapshotReplay) => new SnapshotReplayLaunchData(string.IsNullOrEmpty(replayPath) ? gameConfig.SnapshotFilePath : replayPath!),
                _ => null,
            };
            if (data != null)
                return true;
            error = $"Unknown launch mode \"{mode}\". Use {HalfRemoteClient}, {HalfRemoteServer}, {SinglePlayer} or {SnapshotReplay}.";
            return false;
        }

        private static bool Is(string? value, string mode) => string.Equals(value, mode, StringComparison.OrdinalIgnoreCase);
    }
}
