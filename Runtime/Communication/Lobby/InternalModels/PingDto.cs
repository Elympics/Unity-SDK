using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

namespace Elympics.Communication.Lobby.InternalModels
{
    [MessagePackObject]
    public record PingDto : IFromLobby, IToLobby;
}
