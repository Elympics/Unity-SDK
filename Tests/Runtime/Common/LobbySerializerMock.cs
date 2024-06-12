using System;
using Elympics.Lobby.Models;
using Elympics.Lobby.Serializers;

#nullable enable

namespace Elympics.Tests.Common
{
    internal class LobbySerializerMock : ILobbySerializer
    {
        private Func<IToLobby, byte[]> _serialize = ThrowNotImplementedException<IToLobby, byte[]>;
        private Func<byte[], IFromLobby> _deserialize = ThrowNotImplementedException<byte[], IFromLobby>;

        private static TReturn ThrowNotImplementedException<TArg, TReturn>(TArg _) => throw new NotImplementedException();

        public LobbySerializerMock(Func<IToLobby, byte[]>? serialize = null, Func<byte[], IFromLobby>? deserialize = null)
        {
            if (serialize != null)
                _serialize = serialize;
            if (deserialize != null)
                _deserialize = deserialize;
        }

        public byte[] Serialize(IToLobby message) => _serialize.Invoke(message);
        public IFromLobby Deserialize(byte[] data) => _deserialize.Invoke(data);

        public bool TryGetHumanReadableRepresentation(byte[] data, out string? representation)
        {
            representation = null;
            return false;
        }

        public Methods UpdateMethods(Methods methods)
        {
            Func<IToLobby, byte[]>? oldSerialize = null;
            Func<byte[], IFromLobby>? oldDeserialize = null;
            if (methods.Serialize != null)
            {
                oldSerialize = _serialize;
                _serialize = methods.Serialize;
            }
            if (methods.Deserialize != null)
            {
                oldDeserialize = _deserialize;
                _deserialize = methods.Deserialize;
            }
            return new Methods(oldSerialize, oldDeserialize);
        }

        public void Reset()
        {
            _serialize = ThrowNotImplementedException<IToLobby, byte[]>;
            _deserialize = ThrowNotImplementedException<byte[], IFromLobby>;
        }

        public readonly struct Methods
        {
            public Func<IToLobby, byte[]>? Serialize { get; init; }
            public Func<byte[], IFromLobby>? Deserialize { get; init; }

            public Methods(Func<IToLobby, byte[]>? serialize, Func<byte[], IFromLobby>? deserialize) =>
                (Serialize, Deserialize) = (serialize, deserialize);

            public void Deconstruct(out Func<IToLobby, byte[]>? serialize, out Func<byte[], IFromLobby>? deserialize) =>
                (serialize, deserialize) = (Serialize, Deserialize);
        }
    }
}
