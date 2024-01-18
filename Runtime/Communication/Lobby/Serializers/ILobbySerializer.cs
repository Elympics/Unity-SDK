using Elympics.Lobby.Models;

#nullable enable

namespace Elympics.Lobby.Serializers
{
    internal interface ILobbySerializer
    {
        byte[] Serialize(IToLobby message);
        IFromLobby Deserialize(byte[] data);
        bool TryGetHumanReadableRepresentation(byte[] data, out string? representation);
    }
}
