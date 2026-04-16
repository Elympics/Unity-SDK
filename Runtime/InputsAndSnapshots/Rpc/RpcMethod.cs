using System;
using System.Reflection;

namespace Elympics
{
    internal readonly struct RpcMethod : IEquatable<RpcMethod>
    {
        public MethodInfo MethodInfo { get; }
        public object Target { get; }

        public RpcMethod(MethodInfo methodInfo, object target)
        {
            MethodInfo = methodInfo;
            Target = target;
        }

        public void Call(RpcMethodDetails details, object[] arguments, RpcMetadata metadata)
        {
            if (details.MetadataParameterIndex.HasValue)
            {
                arguments = (object[])arguments.Clone();
                arguments[details.MetadataParameterIndex.Value] = metadata;
            }
            _ = MethodInfo.Invoke(Target, arguments);
        }

        public bool Equals(RpcMethod other) =>
            Equals(MethodInfo, other.MethodInfo) && ReferenceEquals(Target, other.Target);
        public override bool Equals(object obj) =>
            obj is RpcMethod other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(MethodInfo, Target);

        public static bool operator ==(RpcMethod left, RpcMethod right) =>
            left.Equals(right);
        public static bool operator !=(RpcMethod left, RpcMethod right) =>
            !left.Equals(right);

        public override string ToString() =>
            $"{nameof(RpcMethod)} ({MethodInfo.GetFullName()} of {Target})";
    }
}
