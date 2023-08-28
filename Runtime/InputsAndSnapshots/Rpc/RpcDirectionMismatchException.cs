using System.Reflection;

namespace Elympics
{
    public class RpcDirectionMismatchException : ElympicsException
    {
        public RpcDirectionMismatchException(ElympicsRpcProperties rpcProperties, MemberInfo methodInfo)
            : base($"{rpcProperties.Direction} RPC (method name: {methodInfo.Name}) called from a "
                + (rpcProperties.Direction == ElympicsRpcDirection.PlayerToServer ? "non-player" : "non-server"
                + " instance"))
        {
        }
    }
}
