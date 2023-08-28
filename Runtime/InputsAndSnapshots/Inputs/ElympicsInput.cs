using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsInput : ElympicsDataWithTick, IToServer
    {
        [IgnoreMember] public override long Tick { get; set; }
        [Key(1)] public ElympicsPlayer Player { get; set; }
        [Key(2)] public List<KeyValuePair<int, byte[]>> Data { get; set; } = new();
    }
}
