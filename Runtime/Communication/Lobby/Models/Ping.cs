using MessagePack;

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record Ping : IFromLobby, IToLobby;
}
