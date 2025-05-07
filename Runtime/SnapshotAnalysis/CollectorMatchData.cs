#nullable enable

using System;
using System.Collections.Generic;
using MessagePack;

namespace Elympics.SnapshotAnalysis
{
    [MessagePackObject]
    public struct CollectorMatchData
    {
        [Key(0)] public Guid? MatchId { get; set; }
        [Key(1)] public string? QueueName { get; set; }
        [Key(2)] public string? RegionName { get; set; }
        [Key(3)] public IDictionary<Guid, IDictionary<string, string>>? CustomRoomData;
        [Key(4)] public IDictionary<string, string>? CustomMatchmakingData;
    }
}
