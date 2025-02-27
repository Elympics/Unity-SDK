#nullable enable

using System.Collections.Generic;

namespace Elympics.SnapshotAnalysis
{
    public readonly struct SnapshotReplayData
    {
        public SnapshotSaverInitData InitData { get; init; }
        public Dictionary<long, ElympicsSnapshotWithMetadata> Snapshots { get; init; }
    }
}
