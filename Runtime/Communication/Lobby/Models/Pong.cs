using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record Pong : IFromLobby, IToLobby;
}
