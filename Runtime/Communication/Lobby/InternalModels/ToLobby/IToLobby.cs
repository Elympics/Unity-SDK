using Communication.Lobby.Models.ToLobby;
using Elympics.Communication.Lobby.Models.ToLobby;
using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [Union(0, typeof(PingDto))]
    [Union(1, typeof(PongDto))]
    [Union(2, typeof(JoinLobbyDto))]
    [Union(3, typeof(CreateRoomDto))]
    [Union(4, typeof(JoinWithRoomIdDto))]
    [Union(5, typeof(LeaveRoomDto))]
    [Union(6, typeof(ChangeTeamDto))]
    [Union(7, typeof(SetReadyDto))]
    [Union(8, typeof(SetUnreadyDto))]
    [Union(9, typeof(StartMatchmakingDto))]
    [Union(10, typeof(CancelMatchmakingDto))]
    [Union(11, typeof(ShowAuthDto))]
    [Union(12, typeof(JoinWithJoinCodeDto))]
    [Union(13, typeof(SetRoomParametersDto))]
    [Union(14, typeof(WatchRoomsDto))]
    [Union(15, typeof(UnwatchRoomsDto))]
    [Union(16, typeof(RequestRollingsDto))]
    [Union(17, typeof(UpdateCustomPlayerDataDto))]
    public interface IToLobby
    { }
}
