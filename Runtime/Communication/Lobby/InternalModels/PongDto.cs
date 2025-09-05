using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record PongDto : IFromLobby, IToLobby;
}
