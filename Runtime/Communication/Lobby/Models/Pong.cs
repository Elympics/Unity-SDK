using MessagePack;

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record Pong : IFromLobby, IToLobby;
}
