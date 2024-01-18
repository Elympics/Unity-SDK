using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [Union(0, typeof(Ping))]
    [Union(1, typeof(Pong))]
    [Union(2, typeof(JoinLobby))]
    [Union(3, typeof(CreateRoom))]
    [Union(4, typeof(JoinWithRoomId))]
    [Union(5, typeof(LeaveRoom))]
    [Union(6, typeof(ChangeTeam))]
    [Union(7, typeof(SetReady))]
    [Union(8, typeof(SetUnready))]
    [Union(9, typeof(StartMatchmaking))]
    [Union(10, typeof(CancelMatchmaking))]
    // [Union(11, typeof(ShowAuth))] - not needed
    [Union(12, typeof(JoinWithJoinCode))]
    [Union(13, typeof(SetRoomParameters))]
    [Union(14, typeof(WatchRooms))]
    [Union(15, typeof(UnwatchRooms))]
    public interface IToLobby
    { }
}
