using System.Collections.Generic;
using MessagePack;
using Tick = System.Int64;

namespace Elympics
{
    [MessagePackObject]
    public class TickToPlayerInput
    {
        [Key(0)] public Dictionary<Tick, ElympicsSnapshotPlayerInput> Data;
    }
}

