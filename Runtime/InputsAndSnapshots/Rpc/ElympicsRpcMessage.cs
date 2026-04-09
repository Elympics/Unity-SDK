using System;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsRpcMessage
    {
        [Key(0)] public int NetworkId { get; set; }
        [Key(1)] public ushort MethodId { get; set; }
        [Key(2)] public object[] Arguments { get; set; } = Array.Empty<object>();
        [Key(3)] public int Sender { get; set; }
        [Key(4)] public long SentOnTick { get; set; }
        [Key(5)] public long ExecuteNotBeforeTick { get; set; }

        public override string ToString() =>
            $"{nameof(ElympicsRpcMessage)}: Caller network ID {NetworkId}, RPC method ID {MethodId}, arguments count {Arguments.Length}";
    }
}
