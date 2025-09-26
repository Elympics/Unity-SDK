using Elympics.Communication.Rooms.InternalModels.FromRooms;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    [Union(0, typeof(PingDto))]
    [Union(1, typeof(PongDto))]
    [Union(2, typeof(OperationResultDto))]
    [Union(3, typeof(RoomStateChangedDto))]
    [Union(4, typeof(RoomWasLeftDto))]
    [Union(5, typeof(ShowAuthResponseDto))]
    [Union(6, typeof(RoomOperationResultDto))]
    [Union(7, typeof(RoomListChangedDto))]
    [Union(8, typeof(GameDataResponseDto))]
    [Union(9, typeof(RollingsResponseDto))]
    public interface IFromLobby
    { }
}
