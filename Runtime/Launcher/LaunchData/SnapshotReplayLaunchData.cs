#nullable enable

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Elympics
{
    /// <summary>Replays a match recorded with snapshot recording (<see cref="ElympicsGameConfig.RecordSnapshots"/>).</summary>
    [PublicAPI]
    public sealed class SnapshotReplayLaunchData : LaunchData
    {
        /// <summary>
        /// A replay file, or a directory ending with a separator, in which case its newest replay file is used.
        /// </summary>
        public string Path { get; }

        public SnapshotReplayLaunchData(string path) => Path = path ?? string.Empty;

        internal override string ModeName => "SnapshotReplay";

        internal bool IsUrl => Uri.TryCreate(Path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        internal override bool TryValidate(ElympicsGameConfig gameConfig, bool platformCanListen, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            if (string.IsNullOrWhiteSpace(Path))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(Path)} is required.", out failure);
            if (IsUrl)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, "Replays from a URL are not supported yet.", out failure);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{nameof(Path)} must be an http(s) URL on WebGL, which has no local file system.", out failure);
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
