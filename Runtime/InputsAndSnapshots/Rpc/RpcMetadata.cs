using System;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public readonly struct RpcMetadata : IEquatable<RpcMetadata>
    {
        public ElympicsPlayer Sender => ThrowIfUninitializedOrReturn(_sender);

        private readonly bool _initialized;
        private readonly ElympicsPlayer _sender;

        public RpcMetadata(ElympicsPlayer sender)
        {
            _initialized = true;
            _sender = sender;
        }

        private T ThrowIfUninitializedOrReturn<T>(T value)
        {
            if (!_initialized)
                throw new InvalidOperationException("RPC metadata has not been initialized correctly.");
            return value;
        }

        public bool Equals(RpcMetadata other) => _initialized == other._initialized && _sender.Equals(other._sender);
        public override bool Equals(object? obj) => obj is RpcMetadata other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_initialized, _sender);

        public static bool operator ==(RpcMetadata left, RpcMetadata right) => left.Equals(right);
        public static bool operator !=(RpcMetadata left, RpcMetadata right) => !left.Equals(right);

        public override string ToString() => $"{nameof(RpcMetadata)} {{ {nameof(_initialized)}: {_initialized}, {nameof(_sender)}: {_sender} }}";
    }
}
