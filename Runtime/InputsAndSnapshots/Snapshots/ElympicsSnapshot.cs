using System;
using System.Collections.Generic;
using MessagePack;
using PlayerId = System.Int32;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshot : ElympicsDataWithTick, IFromServer
    {
        internal const int DateTimeWeight = 8;

        [IgnoreMember] public sealed override long Tick { get; set; }
        [Key(1)] public DateTime TickStartUtc { get; set; }
        [Key(2)] public FactoryState Factory { get; set; }
        [Key(3)] public List<KeyValuePair<int, byte[]>> Data { get; set; }
        [Key(4)] public Dictionary<PlayerId, TickToPlayerInput> TickToPlayersInputData { get; set; }

        public ElympicsSnapshot()
        { }

        public ElympicsSnapshot(ElympicsSnapshot snapshot)
        {
            Tick = snapshot.Tick;
            TickStartUtc = snapshot.TickStartUtc;
            Factory = snapshot.Factory;
            Data = snapshot.Data;
        }
    }
}
