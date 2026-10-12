#nullable enable
using System;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal class EditorSnapshotReplayInitializer : SnapshotReplayInitializer
    {
        public static Func<IReplayManipulator>? GetManipulator;

        private readonly SnapshotAnalysisRetriever _snapshotRetriever;

        public EditorSnapshotReplayInitializer(SnapshotAnalysisRetriever snapshotRetriever) => _snapshotRetriever = snapshotRetriever;

        public override void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            InitializeInternal(
                server,
                gameConfig,
                behavioursManager,
                _snapshotRetriever,
                (GetManipulator ?? throw new Exception($"{nameof(GetManipulator)} should always be set in editor, are you trying to use {nameof(EditorSnapshotReplayInitializer)} outside of editor?")).Invoke());
            _snapshotRetriever.AddStateMetaData(behavioursManager);
        }
    }
}
