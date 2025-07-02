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
            get => JoiningState is RoomJoiningState.JoinedWithTracking joined
                ? joined.RoomId
                : null;
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

        public RoomJoiner(IRoomsClient roomsClient) =>
            _client = roomsClient;

        public UniTask<Guid> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            bool isEphemeral,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            CompetitivenessConfig? competitivenessConfig = null) =>
            SetupRoomTracking(new RoomJoiningState.Creating(roomName),
                () => _client.CreateRoom(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData, competitivenessConfig));

        public UniTask<Guid> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null) =>
            joinCode != null
                ? JoinRoom(joinCode, teamIndex)
                : JoinRoom(roomId!.Value, teamIndex);

        private async UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex) =>
            await SetupRoomTracking(new RoomJoiningState.JoiningByJoinCode(joinCode),
                () => _client.JoinRoom(joinCode, teamIndex));

        private async UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex) =>
            await SetupRoomTracking(new RoomJoiningState.JoiningByRoomId(roomId),
                () => _client.JoinRoom(roomId, teamIndex));

        private async UniTask<Guid> SetupRoomTracking(RoomJoiningState initialJoiningState, Func<UniTask<Guid>> mainOperationFactory, CancellationToken ct = default)
        {
            ThrowIfAnyRoomJoined();
            JoiningState = initialJoiningState;
            try
            {
                var roomId = await mainOperationFactory();
                UpgradeJoiningToJoined(roomId);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
                await ResultUtils.WaitUntil(() => IsCurrentRoomId(roomId), OperationTimeout, linkedCts.Token);
                return roomId;
            }
            catch
            {
                JoiningState = new RoomJoiningState.NotJoined();
                throw;
            }
        }

        private void UpgradeJoiningToJoined(Guid roomId) =>
            _ = JoiningState switch
            {
                RoomJoiningState.Joined joined when joined.RoomId != roomId => throw new RoomAlreadyJoinedException(joined.RoomId),
                RoomJoiningState.Joined joined when joined.RoomId == roomId => null,
                _ => JoiningState = new RoomJoiningState.JoinedNoTracking(roomId),
            };

        private void ThrowIfAnyRoomJoined() =>
            _ = JoiningState switch
            {
                RoomJoiningState.Joined joined => throw new RoomAlreadyJoinedException(joined.RoomId),
                RoomJoiningState.Joining joining => joining switch
                {
                    RoomJoiningState.JoiningByJoinCode byJoinCode => throw new RoomAlreadyJoinedException(joinCode: byJoinCode.JoinCode, inProgress: true),
                    RoomJoiningState.JoiningByRoomId byRoomId => throw new RoomAlreadyJoinedException(byRoomId.RoomId, inProgress: true),
                    RoomJoiningState.Creating creating => throw new RoomAlreadyJoinedException(roomName: creating.RoomName, inProgress: true),
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
