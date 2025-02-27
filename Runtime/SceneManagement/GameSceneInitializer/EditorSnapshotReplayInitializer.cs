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
            InitializeInternal(
                bot,
                server,
                gameConfig,
                behavioursManager,
                new EditorSnapshotAnalysisRetriever(gameConfig.SnapshotFilePath),
                (GetManipulator ?? throw new Exception($"{nameof(GetManipulator)} should always be set in editor, are you trying to use {nameof(EditorSnapshotReplayInitializer)} outside of editor?")).Invoke());
        }
    }
}
