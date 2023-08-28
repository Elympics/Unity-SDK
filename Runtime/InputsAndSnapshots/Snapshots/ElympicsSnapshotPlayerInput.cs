using System.Collections.Generic;
using MessagePack;
using NetworkId = System.Int32;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshotPlayerInput
    {
        [Key(0)] public List<KeyValuePair<NetworkId, byte[]>> Data { get; set; }
    }
}
