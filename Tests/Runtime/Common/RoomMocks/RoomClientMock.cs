using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Lobby;
using Elympics.Rooms.Models;

#nullable enable

internal class RoomClientMock : IRoomsClient
{
    public event Action<RoomStateChanged>? RoomStateChanged;
    public event Action<LeftRoomArgs>? LeftRoom;
    public event Action<GameDataResponse>? GameDataResponse;
    public event Action<RoomListChanged>? RoomListChanged;

    public SessionConnectionDetails SessionConnectionDetails =>
        _sessionConnectionDetails ?? throw new InvalidOperationException();
    private SessionConnectionDetails? _sessionConnectionDetails;

    public UniTask<Guid> CreateRoom(
        string roomName,
        bool isPrivate,
        bool isEphemeral,
        string queueName,
        bool isSingleTeam,
        IReadOnlyDictionary<string, string> customRoomData,
        IReadOnlyDictionary<string, string> customMatchmakingData,
        CancellationToken ct = default)
    {
        CreateRoomInvokedArgs = (roomName, queueName, isSingleTeam, isPrivate, isEphemeral, customRoomData, customMatchmakingData, ct);
        return RoomIdReturnTask ?? UniTask.FromResult(Guid.Empty);
    }

    public UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex, CancellationToken ct = default)
    {
        JoinRoomWithRoomIdInvokedArgs = (roomId, teamIndex, ct);
        return RoomIdReturnTask ?? UniTask.FromResult(Guid.Empty);
    }

    public UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex, CancellationToken ct = default)
    {
        JoinRoomWithJoinCodeInvokedArgs = (joinCode, teamIndex, ct);
        return RoomIdReturnTask ?? UniTask.FromResult(Guid.Empty);
    }

    public UniTask ChangeTeam(Guid roomId, uint? teamIndex, CancellationToken ct = default)
    {
        SetTeamChangedInvoked.Invoke((roomId, teamIndex));
        return UniTask.CompletedTask;
    }

    public UniTask SetReady(
        Guid roomId,
        byte[]? gameEngineData,
        float[]? matchmakerData,
        CancellationToken ct = default)
    {
        SetReadyInvoked?.Invoke((roomId, gameEngineData, matchmakerData, ct));
        return UniTask.CompletedTask;
    }

    public UniTask SetUnready(Guid roomId, CancellationToken ct = default) => throw new NotImplementedException();

    public UniTask LeaveRoom(Guid roomId, CancellationToken ct = default)
    {
        LeaveRoomArgs = (roomId, ct);
        return UniTask.CompletedTask;
    }

    public UniTask UpdateRoomParams(
        Guid roomId,
        Guid hostId,
        string? roomName,
        bool? isPrivate,
        IReadOnlyDictionary<string, string>? customRoomData,
        IReadOnlyDictionary<string, string>? customMatchmakingData,
        CancellationToken ct = default) => UniTask.CompletedTask;

    public UniTask StartMatchmaking(Guid roomId, Guid hostId)
    {
        StartMatchmakingInvoked?.Invoke((roomId, hostId));
        return UniTask.CompletedTask;
    }

    public UniTask CancelMatchmaking(Guid roomId, CancellationToken ct) => throw new NotImplementedException();
    public UniTask WatchRooms(CancellationToken ct = default) => throw new NotImplementedException();
    public UniTask UnwatchRooms(CancellationToken ct = default) => throw new NotImplementedException();

    public UniTask<Guid>? RoomIdReturnTask { get; set; }

    public (string RoomName, string QueueName, bool IsSingleTeam, bool IsPrivate, bool IsEphemeral, IReadOnlyDictionary<string, string> customRoomData, IReadOnlyDictionary<string, string> customMatchmakingData, CancellationToken Ct)? CreateRoomInvokedArgs { get; private set; }
    public (Guid RoomId, uint? TeamIndex, CancellationToken Ct)? JoinRoomWithRoomIdInvokedArgs { get; private set; }
    public (string JoinCode, uint? TeamIndex, CancellationToken Ct)? JoinRoomWithJoinCodeInvokedArgs { get; private set; }
    public event Action<(Guid RoomId, byte[]? GameEngineData, float[]? MatchmakerData, CancellationToken Ct)>? SetReadyInvoked;
    public event Action<(Guid RoomId, uint? newTeamIndex)> SetTeamChangedInvoked;
    public event Action<(Guid RoomId, Guid HostId)>? StartMatchmakingInvoked;
    public (Guid RoomId, CancellationToken Ct)? LeaveRoomArgs { get; private set; }

    public void InvokeRoomListChanged(RoomListChanged roomStatListChangedChanged) => RoomListChanged?.Invoke(roomStatListChangedChanged);
    public void InvokeRoomStateChanged(RoomStateChanged roomStateChanged) => RoomStateChanged?.Invoke(roomStateChanged);
    public void InvokeLeftRoom(LeftRoomArgs leftRoomArgs) => LeftRoom?.Invoke(leftRoomArgs);

    public void SetSessionConnectionDetails(SessionConnectionDetails? details) => _sessionConnectionDetails = details;

    public void Reset()
    {
        RoomIdReturnTask = null;
        CreateRoomInvokedArgs = null;
        JoinRoomWithRoomIdInvokedArgs = null;
        JoinRoomWithJoinCodeInvokedArgs = null;
        SetReadyInvoked = null;
        StartMatchmakingInvoked = null;
        LeaveRoomArgs = null;
        _sessionConnectionDetails = null;
    }
}
