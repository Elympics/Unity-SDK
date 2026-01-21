using System;
using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshotWithMetadata : ElympicsSnapshot
    {
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

        [SerializationConstructor]
        public ElympicsSnapshotWithMetadata(long tick, DateTime tickStartUtc, FactoryState factory, Dictionary<int, byte[]> data, Dictionary<int, TickToPlayerInput> tickToPlayersInputData, DateTime tickEndUtc, List<ElympicsBehaviourMetadata> metadata) : base(tick, tickStartUtc, factory, data, tickToPlayersInputData)
        {
            TickEndUtc = tickEndUtc;
            Metadata = metadata;
        }

        [Key(5)] public DateTime TickEndUtc { get; set; }
        [Key(6)] public List<ElympicsBehaviourMetadata> Metadata { get; set; }
    }
}
