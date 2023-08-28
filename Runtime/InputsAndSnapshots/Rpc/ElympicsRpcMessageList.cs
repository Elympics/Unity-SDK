using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsRpcMessageList : ElympicsDataWithTick, IToServer, IFromServer
    {
        [IgnoreMember] public override long Tick { get; set; }
        [Key(1)] public List<ElympicsRpcMessage> Messages { get; set; } = new();

    }

}
