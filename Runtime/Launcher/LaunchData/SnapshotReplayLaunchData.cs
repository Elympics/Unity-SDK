#nullable enable

using System;
using Elympics.SnapshotAnalysis.Retrievers;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public sealed class SnapshotReplayLaunchData : LaunchData
    {
        /// <summary>
        /// An http(s) URL of a replay file, downloaded before the gameplay scene loads. Otherwise a local replay file,
        /// or a directory ending with a separator, in which case its newest replay file is used. Only URLs work on WebGL.
        /// </summary>
        public string Path { get; }

        public SnapshotReplayLaunchData(string path) => Path = path ?? string.Empty;

        /// <summary>A replay the caller has already loaded. No path is read; only its game version is checked.</summary>
        internal SnapshotReplayLaunchData(SnapshotAnalysisRetriever preloaded)
        {
            Path = string.Empty;
            Preloaded = preloaded;
        }

        internal SnapshotAnalysisRetriever? Preloaded { get; }

        internal override string ModeName => "SnapshotReplay";

        internal bool IsUrl => Uri.TryCreate(Path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        internal override bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            if (Preloaded != null)
                return true;
            if (string.IsNullOrWhiteSpace(Path))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(Path)} is required.", out failure);
            if (IsUrl)
                return true;
            if (!platform.HasLocalFiles)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{nameof(Path)} {ElympicsLaunchPlatform.NoLocalFilesMessage}.", out failure);
            if (!SnapshotReplayLoader.LocalReplayExists(Path, out var message))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, message, out failure);
            return true;
        }

        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context)
        {
            var retriever = context.SnapshotRetriever ?? throw new ElympicsException("Snapshot replay launch without a loaded replay.");
#if UNITY_EDITOR
            return new EditorSnapshotReplayInitializer(retriever);
#else
            return new PlayerSnapshotReplayInitializer(retriever);
#endif
        }
    }
}
