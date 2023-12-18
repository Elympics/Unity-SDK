using System;
using System.Reflection;

namespace Elympics
{
    internal readonly struct RpcMethod : IEquatable<RpcMethod>
    {
        private readonly MethodInfo _methodInfo;
        private readonly object _target;
        private readonly uint? _metadataParameterIndex;

        public RpcMethod(MethodInfo methodInfo, object target)
        {
            _methodInfo = methodInfo;
            _target = target;

            _metadataParameterIndex = null;
            var parameters = _methodInfo.GetParameters();
            for (var i = 0u; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.ParameterType == typeof(RpcMetadata)
                    && parameter.IsOptional
                    && parameter.HasDefaultValue
                    && (parameter.DefaultValue == null || (RpcMetadata)parameter.DefaultValue == default))
                {
                    _metadataParameterIndex = i;
                    break;
                }
            }
        }

        public void Call(object[] arguments, RpcMetadata metadata)
        {
            if (_metadataParameterIndex.HasValue)
            {
                arguments = (object[])arguments.Clone();
                arguments[_metadataParameterIndex.Value] = metadata;
            }
            _ = _methodInfo.Invoke(_target, arguments);
        }

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

        public override string ToString() =>
            $"{nameof(RpcMethod)} ({_methodInfo.DeclaringType!.FullName}.{_methodInfo.Name} of {_target})";
    }
}
