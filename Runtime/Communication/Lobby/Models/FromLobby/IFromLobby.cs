using Communication.Lobby.Models.ToLobby;
using Elympics.Communication.Lobby.Models.FromLobby;
using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [Union(0, typeof(Ping))]
    [Union(1, typeof(Pong))]
    [Union(2, typeof(OperationResult))]
    [Union(3, typeof(RoomStateChanged))]
    [Union(4, typeof(RoomWasLeft))]
    [Union(5, typeof(ShowAuthResponse))]
    [Union(6, typeof(RoomIdOperationResult))]
    [Union(7, typeof(RoomListChanged))]
    [Union(8, typeof(GameDataResponse))]
    [Union(9, typeof(RollingsResponse))]
    public interface IFromLobby
    { }
}
