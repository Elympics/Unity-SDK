using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsInputList : IToServer
    {
        [Key(0)] public IList<ElympicsInput> Values { get; set; }
        [Key(1)] public long LastReceivedSnapshot;
    }
}
