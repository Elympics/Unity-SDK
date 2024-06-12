using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Serializers
{
    internal class MessagePackLobbySerializer : ILobbySerializer
    {
        private readonly MessagePackSerializerOptions? _options;

        public byte[] Serialize(IToLobby message) =>
            MessagePackSerializer.Serialize(message, _options);

        public IFromLobby Deserialize(byte[] data) =>
            MessagePackSerializer.Deserialize<IFromLobby>(data, _options);

        public bool TryGetHumanReadableRepresentation(byte[] data, out string? representation)
        {
            representation = null;
            try
            {
                representation = MessagePackSerializer.ConvertToJson(data, _options);
                return true;
            }
            catch (MessagePackSerializationException e)
            {
                _ = ElympicsLogger.LogException(e);
                return false;
            }
        }

        public MessagePackLobbySerializer(MessagePackSerializerOptions? options = null) =>
            _options = options;
    }
}
