using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Lobby;

#nullable enable

namespace Elympics
{
    internal class RoomJoiner : IRoomJoiner
    {
        private CancellationTokenSource _cts = new();

        public TimeSpan OperationTimeout { private get; init; } = TimeSpan.FromSeconds(5);

        private readonly IRoomsClient _client;

        private RoomJoiningState _joiningState = new RoomJoiningState.NotJoined();
        private RoomJoiningState JoiningState
        {
            get => _joiningState;
            set
            {
                _joiningState = value;
                JoiningStateChanged?.Invoke(value);
            }
        }
        public event Action<RoomJoiningState>? JoiningStateChanged;

        public Guid? CurrentRoomId
        {
            get => JoiningState is RoomJoiningState.JoinedWithTracking joined ? joined.RoomId : null;
            set
            {
                if (value == CurrentRoomId)
                    return;
                if (value is null)
                    JoiningState = new RoomJoiningState.NotJoined();
                else
                    JoiningState = new RoomJoiningState.JoinedWithTracking(value.Value);
            }
        }

        private bool IsCurrentRoomId(Guid roomId) =>
            JoiningState is RoomJoiningState.JoinedWithTracking joined && joined.RoomId == roomId;

        public RoomJoiner(IRoomsClient roomsClient)
        {
            _client = roomsClient;
        }

        public UniTask<Guid> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            bool isEphemeral,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            RoomBetAmount? betDetails = null,
            TournamentDetails? tournamentDetails = null)
        {
            ThrowIfAnyRoomJoined();
            JoiningState = new RoomJoiningState.Creating(roomName);
            var ackTask = _client.CreateRoom(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData, betDetails, tournamentDetails);
            return SetupRoomTracking(ackTask);
        }

        public UniTask<Guid> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null)
        {
            ThrowIfAnyRoomJoined();
            return joinCode != null
                ? JoinRoom(joinCode, teamIndex)
                : JoinRoom(roomId!.Value, teamIndex);
        }

        private async UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex)
        {
            JoiningState = new RoomJoiningState.JoiningByJoinCode(joinCode);
            return await SetupRoomTracking(_client.JoinRoom(joinCode, teamIndex));
        }

        private async UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex)
        {
            JoiningState = new RoomJoiningState.JoiningByRoomId(roomId);
            return await SetupRoomTracking(_client.JoinRoom(roomId, teamIndex));
        }

        private async UniTask<Guid> SetupRoomTracking(UniTask<Guid> mainOperation, CancellationToken ct = default)
        {
            var roomId = await mainOperation;
            if (IsCurrentRoomId(roomId))
                throw new RoomAlreadyJoinedException(roomId);
            JoiningState = new RoomJoiningState.JoinedNoTracking(roomId);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            try
            {
                await ResultUtils.WaitUntil(() => IsCurrentRoomId(roomId), OperationTimeout, linkedCts.Token);
                return roomId;
            }
            catch
            {
                JoiningState = new RoomJoiningState.NotJoined();
                throw;
            }
        }

        private void ThrowIfAnyRoomJoined() =>
            _ = JoiningState switch
            {
                RoomJoiningState.Joined joined => throw new RoomAlreadyJoinedException(joined.RoomId),
                RoomJoiningState.Joining joining => joining switch
                {
                    RoomJoiningState.JoiningByJoinCode byJoinCode => throw new RoomAlreadyJoinedException(joinCode: byJoinCode.JoinCode, inProgress: true),
                    RoomJoiningState.JoiningByRoomId byRoomId => throw new RoomAlreadyJoinedException(byRoomId.RoomId, inProgress: true),
                    // TODO: include created room name in the exception
                    _ => throw new RoomAlreadyJoinedException(inProgress: true),
                },
                _ => "",
            };

        public void Reset()
        {
            _cts.Cancel();
            _cts.Dispose();
            JoiningState = new RoomJoiningState.NotJoined();
            _cts = new CancellationTokenSource();
        }
    }
}
