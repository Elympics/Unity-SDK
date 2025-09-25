#nullable enable

using System;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal class EditorSnapshotReplayInitializer : SnapshotReplayInitializer
    {
        public static Func<IReplayManipulator>? GetManipulator;

        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsSinglePlayer singlePlayer,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            var retriever = new EditorSnapshotAnalysisRetriever(gameConfig.SnapshotFilePath);
            var replayVersion = retriever.RetrieveInitData().GameVersion;
            var currentVersion = ElympicsConfig.LoadCurrentElympicsGameConfig().GameVersion;

            if (replayVersion != currentVersion)
                throw new Exception($"Game version mismatch. Replay was recorded using game version {replayVersion} and current game version is {currentVersion}. Use a matching version of the game to watch this replay.");

            InitializeInternal(
                bot,
                server,
                gameConfig,
                behavioursManager,
                retriever,
                (GetManipulator ?? throw new Exception($"{nameof(GetManipulator)} should always be set in editor, are you trying to use {nameof(EditorSnapshotReplayInitializer)} outside of editor?")).Invoke());
            retriever.AddStateMetaData(behavioursManager);
        }
    }
}
