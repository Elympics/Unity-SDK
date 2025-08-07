using System;
using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshotWithMetadata : ElympicsSnapshot
    {
        public ElympicsSnapshotWithMetadata() =>
            Metadata = new List<ElympicsBehaviourMetadata>();

        public ElympicsSnapshotWithMetadata(ElympicsSnapshot snapshot, DateTime tickEndUtc) : base(snapshot)
        {
            TickEndUtc = tickEndUtc;
            Metadata = new List<ElympicsBehaviourMetadata>(snapshot.Data.Count);
        }

        public ElympicsSnapshotWithMetadata(ElympicsSnapshotWithMetadata snapshot, DateTime tickEndUtc) : base(snapshot)
        {
            TickEndUtc = tickEndUtc;
            Metadata = new List<ElympicsBehaviourMetadata>(snapshot.Metadata);
        }

        [Key(5)] public DateTime TickEndUtc { get; set; }
        [Key(6)] public List<ElympicsBehaviourMetadata> Metadata { get; set; }
    }
}
