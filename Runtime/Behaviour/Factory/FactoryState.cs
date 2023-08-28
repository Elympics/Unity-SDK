using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class FactoryState
    {
        [Key(0)] public List<KeyValuePair<int, byte[]>> Parts;
    }
}
