#nullable enable

using Cysharp.Threading.Tasks;

namespace Elympics.SnapshotAnalysis
{
    internal class SinglePlayerSnapshotAnalysisCollector : SnapshotAnalysisCollector
    {
        public override void CaptureSnapshot(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata snapshot)
        { }
        protected override void SaveInitData(SnapshotSaverInitData initData)
        { }
        protected override UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer) => new();
        protected override void SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots) { }
    }
}
