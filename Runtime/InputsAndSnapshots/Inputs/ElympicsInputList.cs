using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public struct ElympicsInputList : IToServer
    {
        [Key(0)] public List<ElympicsInput> Values { get; set; }
    }
}
