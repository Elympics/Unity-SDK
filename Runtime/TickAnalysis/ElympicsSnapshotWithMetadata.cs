using System;
using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshotWithMetadata : ElympicsSnapshot
    {
        public ElympicsSnapshotWithMetadata()
        {
            Metadata = new List<ElympicsBehaviourMetadata>();
        }

        public ElympicsSnapshotWithMetadata(ElympicsSnapshot snapshot) : base(snapshot)
        {
            Metadata = new List<ElympicsBehaviourMetadata>();
        }

        public ElympicsSnapshotWithMetadata(ElympicsSnapshotWithMetadata snapshot) : base(snapshot)
        {
            Metadata = snapshot.Metadata;
        }

        [Key(5)] public DateTime TickEndUtc { get; set; }
        [Key(6)] public long FixedUpdateNumber { get; set; }
        [Key(7)] public List<ElympicsBehaviourMetadata> Metadata { get; set; }
    }
}
