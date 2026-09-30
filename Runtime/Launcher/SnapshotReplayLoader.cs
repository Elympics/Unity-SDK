#nullable enable

using System;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    /// <summary>Loads the replay of a <see cref="SnapshotReplayLaunchData"/> before its gameplay scene loads.</summary>
    internal static class SnapshotReplayLoader
    {
        public static bool LocalReplayExists(string path, out string error) => EditorSnapshotAnalysisRetriever.TryResolvePath(path, out _, out error);

        public static bool TryLoad(SnapshotReplayLaunchData data, ElympicsGameConfig gameConfig, out SnapshotAnalysisRetriever? retriever, out ElympicsLaunchFailure? failure)
        {
            retriever = null;
            try
            {
                retriever = new EditorSnapshotAnalysisRetriever(data.Path);
            }
            catch (Exception e)
            {
                failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload, $"Could not read replay {data.Path}: {e.Message}");
                return false;
            }
            return TryCheckVersion(retriever, gameConfig, out failure);
        }

        /// <summary>A replay plays back only in the game version it was recorded with.</summary>
        internal static bool TryCheckVersion(SnapshotAnalysisRetriever retriever, ElympicsGameConfig gameConfig, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            var replayVersion = retriever.RetrieveInitData().GameVersion;
            if (replayVersion == gameConfig.GameVersion)
                return true;
            failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload,
                $"Game version mismatch. The replay was recorded with game version {replayVersion} and the current game version is {gameConfig.GameVersion}. "
                + "Use a matching version of the game to watch this replay.");
            return false;
        }
    }
}
