using MessagePack;

namespace Elympics
{
    [Union(0, typeof(ElympicsSnapshot))]
    [Union(1, typeof(ElympicsInput))]
    [Union(2, typeof(ElympicsRpcMessageList))]
    public abstract class ElympicsDataWithTick
    {
        [Key(0)] public abstract long Tick { get; set; }
    }
}
