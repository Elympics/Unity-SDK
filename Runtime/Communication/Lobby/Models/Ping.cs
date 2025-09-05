using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record Ping : IFromLobby, IToLobby;
}
