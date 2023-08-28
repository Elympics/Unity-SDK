using System;
using System.Reflection;

namespace Elympics
{
    internal readonly struct RpcMethod : IEquatable<RpcMethod>
    {
        private readonly MethodInfo _methodInfo;
        private readonly object _target;

        public RpcMethod(MethodInfo methodInfo, object target)
        {
            _methodInfo = methodInfo;
            _target = target;
        }

        public void Call(object[] arguments) => _methodInfo.Invoke(_target, arguments);

        public bool Equals(RpcMethod other) =>
            Equals(_methodInfo, other._methodInfo) && ReferenceEquals(_target, other._target);
        public override bool Equals(object obj) =>
            obj is RpcMethod other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(_methodInfo, _target);

        public static bool operator ==(RpcMethod left, RpcMethod right) =>
            left.Equals(right);
        public static bool operator !=(RpcMethod left, RpcMethod right) =>
            !left.Equals(right);
    }
}
