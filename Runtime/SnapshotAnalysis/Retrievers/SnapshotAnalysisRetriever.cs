#nullable enable

using System.Collections.Generic;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal abstract class SnapshotAnalysisRetriever
    {
        protected SnapshotReplayData Replay;
        public virtual SnapshotSaverInitData RetrieveInitData() => Replay.InitData;
        public virtual Dictionary<long, ElympicsSnapshotWithMetadata> RetrieveSnapshots() => Replay.Snapshots;

        /// <summary>Feeds every snapshot's metadata to <paramref name="behaviourManager"/>, then restores the first snapshot. Used by the Editor replay controller.</summary>
        public void AddStateMetaData(ElympicsBehavioursManager behaviourManager)
        {
            long? firstTick = null;
            foreach (var snapshotWithMeta in Replay.Snapshots)
            {
                firstTick ??= snapshotWithMeta.Key;
                behaviourManager.ApplySnapshot(snapshotWithMeta.Value, ElympicsBehavioursManager.StatePredictability.Both, true);
                behaviourManager.AddStateMetaData(snapshotWithMeta.Value);
            }
            behaviourManager.ApplySnapshot(Replay.Snapshots[firstTick!.Value]);
        }
    }
}
