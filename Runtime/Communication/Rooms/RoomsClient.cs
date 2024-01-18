using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Lobby.Models;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class RoomsClient : IRoomsClient
    {
        public event Action<RoomStateChanged>? RoomStateChanged;
        public event Action<LeftRoomArgs>? LeftRoom;
        public event Action<RoomListChanged>? RoomListChanged;

        public SessionConnectionDetails SessionConnectionDetails =>
            Session?.ConnectionDetails ?? throw new InvalidOperationException("Missing WebSocket session object.");

        public IWebSocketSessionInternal? Session
        {
            private get => _session;
            set
            {
                if (_session != null)
                    _session.MessageReceived -= HandleMessage;
                _session = value;
                if (_session != null)
                    _session.MessageReceived += HandleMessage;
            }
        }
        private IWebSocketSessionInternal? _session;

        private void HandleMessage(IFromLobby message)
        {
            switch (message)
            {
                case RoomStateChanged stateChanged:
                    RoomStateChanged?.Invoke(stateChanged);
                    return;
                case RoomWasLeft roomLeft:
                    LeftRoom?.Invoke(new LeftRoomArgs(roomLeft.RoomId, roomLeft.Reason));
                    return;
                case RoomListChanged roomListChanged:
                    RoomListChanged?.Invoke(roomListChanged);
                    return;
                default:
                    return;
            }
        }

        public UniTask<Guid> CreateRoom(
            string roomName,
            bool isPrivate,
            bool isEphemeral,
            string queueName,
            bool isSingleTeam,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            CancellationToken ct = default) =>
            ExecuteOperation<RoomIdOperationResult>(new CreateRoom(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData), ct).ContinueWith(result => result.RoomId);

        public UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex, CancellationToken ct = default) =>
            ExecuteOperation<RoomIdOperationResult>(new JoinWithRoomId(roomId, teamIndex), ct)
                .ContinueWith(result => result.RoomId);

        public UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex, CancellationToken ct = default) =>
            ExecuteOperation<RoomIdOperationResult>(new JoinWithJoinCode(joinCode, teamIndex), ct)
                .ContinueWith(result => result.RoomId);

        public UniTask ChangeTeam(Guid roomId, uint? teamIndex, CancellationToken ct = default) =>
            ExecuteOperation(new ChangeTeam(roomId, teamIndex), ct);

        public UniTask SetReady(Guid roomId, byte[] gameEngineData, float[] matchmakerData, CancellationToken ct = default) =>
            ExecuteOperation(new SetReady(roomId, gameEngineData, matchmakerData), ct);

        public UniTask SetUnready(Guid roomId, CancellationToken ct = default) =>
            ExecuteOperation(new SetUnready(roomId), ct);

        public UniTask LeaveRoom(Guid roomId, CancellationToken ct = default) =>
            ExecuteOperation(new LeaveRoom(roomId), ct);

        public UniTask UpdateRoomParams(
            Guid roomId,
            Guid hostId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData,
            CancellationToken ct = default) =>
            ExecuteOperationHostOnly(hostId, new SetRoomParameters(roomId, roomName, isPrivate, customRoomData, customMatchmakingData), ct);

        public UniTask StartMatchmaking(Guid roomId, Guid hostId) =>
            ExecuteOperationHostOnly(hostId, new StartMatchmaking(roomId), default);

        public UniTask CancelMatchmaking(Guid roomId, CancellationToken ct = default) =>
            ExecuteOperation(new CancelMatchmaking(roomId), ct);
        public UniTask WatchRooms(CancellationToken ct = default) => ExecuteOperation(new WatchRooms(), ct);
        public UniTask UnwatchRooms(CancellationToken ct = default) => ExecuteOperation(new UnwatchRooms(), ct);

        private UniTask ExecuteOperation(LobbyOperation message, CancellationToken ct) =>
            ExecuteOperation<OperationResult>(message, ct);

        private async UniTask<T> ExecuteOperation<T>(LobbyOperation message, CancellationToken ct)
            where T : OperationResult
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            ct.ThrowIfCancellationRequested();
            var result = await Session.ExecuteOperation(message, ct);
            if (result is T expectedResult)
                return expectedResult;
            throw new UnexpectedRoomResultException(typeof(T), result.GetType());
        }

        private UniTask ExecuteOperationHostOnly(Guid? roomHostId, LobbyOperation message, CancellationToken ct, [CallerMemberName] string methodName = "")
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            if (roomHostId == null)
                throw new RoomPrivilegeException($"Cannot call ${methodName} on rooms without host.");
            var expectedUserId = SessionConnectionDetails.AuthData.UserId;
            if (expectedUserId != null && roomHostId != expectedUserId)
                throw new RoomPrivilegeException($"Only hosts can call ${methodName} method.");

            return Session.ExecuteOperation(message, ct);
        }
    }
}
