using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record PingDto : IFromLobby, IToLobby;
}
