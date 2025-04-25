using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Lobby;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal interface IRoomsClient
    {
        event Action<GameDataResponse>? GameDataResponse;
        event Action<RoomListChanged>? RoomListChanged;
        event Action<RoomStateChanged>? RoomStateChanged;
        event Action<LeftRoomArgs>? LeftRoom;

        SessionConnectionDetails SessionConnectionDetails { get; }

        UniTask<Guid> CreateRoom(
            string roomName,
            bool isPrivate,
            bool isEphemeral,
            string queueName,
            bool isSingleTeam,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            RoomBetAmount? betDetails = null,
            CancellationToken ct = default);
        UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex, CancellationToken ct = default);
        UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex, CancellationToken ct = default);
        UniTask ChangeTeam(Guid roomId, uint? teamIndex, CancellationToken ct = default);
        UniTask SetReady(Guid roomId, byte[] gameEngineData, float[] matchmakerData, CancellationToken ct = default);
        UniTask SetUnready(Guid roomId, CancellationToken ct = default);
        UniTask LeaveRoom(Guid roomId, CancellationToken ct = default);
        UniTask UpdateRoomParams(
            Guid roomId,
            Guid hostId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData,
            RoomBetAmount? betDetailsSlim = null,
            CancellationToken ct = default);
        UniTask StartMatchmaking(Guid roomId, Guid hostId);
        UniTask CancelMatchmaking(Guid roomId, CancellationToken ct = default);
        UniTask WatchRooms(CancellationToken ct = default);
        UniTask UnwatchRooms(CancellationToken ct = default);
    }
}
