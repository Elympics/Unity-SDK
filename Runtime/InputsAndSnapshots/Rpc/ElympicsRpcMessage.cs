using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsRpcMessage
    {
        [Key(0)] public int NetworkId;
        [Key(1)] public ushort MethodId;
        [Key(2)] public object[] Arguments;

        public ElympicsRpcMessage()
        {
        }

        public ElympicsRpcMessage(int networkId, ushort methodId, params object[] arguments)
        {
            NetworkId = networkId;
            MethodId = methodId;
            Arguments = arguments;
        }

        public override string ToString() =>
            $"{nameof(ElympicsRpcMessage)}: Caller network ID {NetworkId}, RPC method ID {MethodId}, arguments count {Arguments.Length}";
    }
}
