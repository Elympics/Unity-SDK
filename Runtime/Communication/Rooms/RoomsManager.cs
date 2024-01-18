using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class RoomsManager : IRoomsManager
    {
        #region Aggregated room events

        public event Action<RoomListUpdatedArgs>? RoomListUpdated;
        public event Action<JoinedRoomUpdatedArgs>? JoinedRoomUpdated;

        public event Action<JoinedRoomArgs>? JoinedRoom;
        public event Action<LeftRoomArgs>? LeftRoom;

        public event Action<UserJoinedArgs>? UserJoined;
        public event Action<UserLeftArgs>? UserLeft;
        public event Action<UserCountChangedArgs>? UserCountChanged;
        public event Action<HostChangedArgs>? HostChanged;
        public event Action<UserReadinessChangedArgs>? UserReadinessChanged;
        public event Action<UserChangedTeamArgs>? UserChangedTeam;
        public event Action<CustomRoomDataChangedArgs>? CustomRoomDataChanged;
        public event Action<RoomPublicnessChangedArgs>? RoomPublicnessChanged;
        public event Action<RoomNameChangedArgs>? RoomNameChanged;

        public event Action<MatchmakingDataChangedArgs>? MatchmakingDataChanged;
        public event Action<MatchmakingStartedArgs>? MatchmakingStarted;
        public event Action<MatchmakingEndedArgs>? MatchmakingEnded;
        public event Action<MatchDataReceivedArgs>? MatchDataReceived;
        public event Action<CustomMatchmakingDataChangedArgs>? CustomMatchmakingDataChanged;

        #endregion Aggregated room events

        public TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

        private readonly Dictionary<Guid, IRoom> _rooms = new();
        private readonly IRoomJoiningQueue _joiningQueue;
        private CancellationTokenSource _cts = new();

        private readonly IMatchLauncher _matchLauncher;
        private readonly IRoomsClient _client;

        private readonly RoomStateDiff _stateDiff = new();

        public event Func<IRoom, IRoom>? RoomSetUp
        {
            add
            {
                if (value != null && _roomDecorators.IndexOf(value) == -1)
                    _roomDecorators.Add(value);
            }
            remove
            {
                if (value != null)
                    _ = _roomDecorators.RemoveAll(x => x == value);
            }
        }
        private readonly List<Func<IRoom, IRoom>> _roomDecorators = new();

        public RoomsManager(IMatchLauncher matchLauncher, IRoomsClient roomsClient, IRoomJoiningQueue? joiningQueue = null)
        {
            _matchLauncher = matchLauncher;
            _client = roomsClient;
            _joiningQueue = joiningQueue ?? new RoomJoiningQueue();
            SubscribeClient();
        }

        private void SubscribeClient()
        {
            _client.RoomListChanged += HandleRoomListChanged;
            _client.RoomStateChanged += HandleJoinedRoomUpdated;
            _client.LeftRoom += HandleLeftRoom;
        }

        private void HandleRoomListChanged(RoomListChanged roomListChanged)
        {
            foreach (var listedRoomChange in roomListChanged.Changes)
            {
                var (roomId, stateUpdate) = listedRoomChange;
                if (stateUpdate == null)
                {
                    if (_rooms.Remove(roomId, out var removedRoom))
                        removedRoom.Dispose();
                    continue;
                }

                if (_rooms.TryGetValue(roomId, out var existingRoom))
                {
                    if (!existingRoom.IsJoined)
                        existingRoom.UpdateState(stateUpdate);
                }
                else
                    AddRoomToDictionary(new Room(_matchLauncher, _client, roomId, stateUpdate));
            }
            RoomListUpdated?.Invoke(new RoomListUpdatedArgs(roomListChanged.Changes.Select(change => change.RoomId).ToList()));
        }

        public void Reset()
        {
            _cts.Cancel();
            _cts.Dispose();
            var clearedRooms = _rooms.ToArray();
            _rooms.Clear();
            _joiningQueue.Clear();
            foreach (var (roomId, room) in clearedRooms)
            {
                room.Dispose();
                LeftRoom?.Invoke(new LeftRoomArgs(roomId, LeavingReason.RoomClosed));
            }
            _cts = new CancellationTokenSource();
        }

        private void HandleJoinedRoomUpdated(RoomStateChanged roomState)
        {
            var roomId = roomState.RoomId;
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.UpdateState(roomState, _stateDiff);
                if (!room.IsJoined)
                {
                    room.IsJoined = true;
                    _stateDiff.InitializedState = true;
                }
            }
            else
            {
                AddRoomToDictionary(new Room(_matchLauncher, _client, roomId, roomState)
                {
                    IsJoined = true
                });
                SetStateDiffToInitializeState();
            }
            InvokeEventsBasedOnStateDiff(roomId, _stateDiff);
            PlayAvailableMatchIfApplicable(roomId, _stateDiff);
            return;

            void SetStateDiffToInitializeState()
            {
                _stateDiff.Reset();
                _stateDiff.UpdatedState = true;
                _stateDiff.InitializedState = true;
            }
        }

        private void AddRoomToDictionary(IRoom room)
        {
            foreach (var decorator in _roomDecorators)
                room = decorator(room);
            _rooms.Add(room.RoomId, room);
        }

        internal void InvokeEventsBasedOnStateDiff(Guid roomId, RoomStateDiff stateDiff)
        {
            if (!stateDiff.UpdatedState)
                return;
            JoinedRoomUpdated?.Invoke(new JoinedRoomUpdatedArgs(roomId));
            if (stateDiff.InitializedState)
                JoinedRoom?.Invoke(new JoinedRoomArgs(roomId));
            foreach (var userThatJoined in stateDiff.UsersThatJoined)
                UserJoined?.Invoke(new UserJoinedArgs(roomId, userThatJoined));
            foreach (var userThatLeft in stateDiff.UsersThatLeft)
                UserLeft?.Invoke(new UserLeftArgs(roomId, userThatLeft));
            if (stateDiff.NewUserCount != null)
                UserCountChanged?.Invoke(new UserCountChangedArgs(roomId, stateDiff.NewUserCount.Value));
            if (stateDiff.NewHost != null)
                HostChanged?.Invoke(new HostChangedArgs(roomId, stateDiff.NewHost.Value));
            foreach (var (userId, isReady) in stateDiff.UsersThatChangedReadiness)
                UserReadinessChanged?.Invoke(new UserReadinessChangedArgs(roomId, userId, isReady));
            foreach (var (userId, teamIndex) in stateDiff.UsersThatChangedTeams)
                UserChangedTeam?.Invoke(new UserChangedTeamArgs(roomId, userId, teamIndex));
            if (stateDiff.NewCustomRoomData.Count > 0)
                foreach (var (newKey, newValue) in stateDiff.NewCustomRoomData)
                    CustomRoomDataChanged?.Invoke(new CustomRoomDataChangedArgs(roomId, newKey, newValue));
            if (stateDiff.NewIsPrivate is not null)
                RoomPublicnessChanged?.Invoke(new RoomPublicnessChangedArgs(roomId, stateDiff.NewIsPrivate.Value));
            if (stateDiff.NewRoomName is not null)
                RoomNameChanged?.Invoke(new RoomNameChangedArgs(roomId, stateDiff.NewRoomName));
            if (_stateDiff.MatchmakingStarted)
                MatchmakingStarted?.Invoke(new MatchmakingStartedArgs(roomId));
            if (_stateDiff.MatchmakingEnded)
                MatchmakingEnded?.Invoke(new MatchmakingEndedArgs(roomId));
            if (_stateDiff.UpdatedMatchmakingData)
                MatchmakingDataChanged?.Invoke(new MatchmakingDataChangedArgs(roomId));
            if (_stateDiff.MatchDataArgs != null)
                MatchDataReceived?.Invoke(_stateDiff.MatchDataArgs);
            if (stateDiff.NewCustomMatchmakingData.Count > 0)
                foreach (var (newKey, newValue) in stateDiff.NewCustomMatchmakingData)
                    CustomMatchmakingDataChanged?.Invoke(new CustomMatchmakingDataChangedArgs(roomId, newKey, newValue));
        }

        private void PlayAvailableMatchIfApplicable(Guid roomId, RoomStateDiff stateDiff)
        {
            if (stateDiff.MatchDataArgs == null)
                return;
            if (!_matchLauncher.ShouldLoadGameplaySceneAfterMatchmaking || _matchLauncher.IsCurrentlyInMatch)
                return;
            _rooms[roomId].PlayAvailableMatch();
        }

        private void HandleLeftRoom(LeftRoomArgs args)
        {
            if (_rooms.TryGetValue(args.RoomId, out var room))
                room.IsJoined = false;
            LeftRoom?.Invoke(args);
        }

        public bool TryGetAvailableRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (_rooms.TryGetValue(roomId, out var availableRoom) && !availableRoom.IsJoined)
                room = availableRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListAvailableRooms() => _rooms.Values.Where(room => !room.IsJoined).ToList();

        public bool TryGetJoinedRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (_rooms.TryGetValue(roomId, out var joinedRoom) && joinedRoom.IsJoined)
                room = joinedRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListJoinedRooms() => _rooms.Values.Where(room => room.IsJoined).ToList();

        public UniTask StartTrackingAvailableRooms() => _client.WatchRooms();

        public UniTask StopTrackingAvailableRooms() => _client.UnwatchRooms();

        public UniTask<IRoom> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null)
        {
            if (roomName == null)
                throw new ArgumentNullException(nameof(roomName));
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));
            customRoomData ??= new Dictionary<string, string>();
            customMatchmakingData ??= new Dictionary<string, string>();
            var ackTask = _client.CreateRoom(roomName, isPrivate, false, queueName, isSingleTeam, customRoomData, customMatchmakingData);
            return SetupRoomTracking(ackTask);
        }

        public UniTask<IRoom> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null)
        {
            if (joinCode != null)
                return JoinRoom(joinCode, teamIndex);
            if (roomId != null)
                return JoinRoom(roomId.Value, teamIndex);
            throw new ArgumentException($"{nameof(roomId)} and {nameof(joinCode)} cannot be null at the same time");
        }

        public async UniTask<IRoom> StartQuickMatch(string queueName, byte[]? gameEngineData = null, float[]? matchmakerData = null, CancellationToken ct = default)
        {
            const string quickMatchRoomName = "quick-match";

            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));
            gameEngineData ??= Array.Empty<byte>();
            matchmakerData ??= Array.Empty<float>();
            ct.ThrowIfCancellationRequested();

            IRoom? room = null;
            try
            {
                var ackTask = _client.CreateRoom(quickMatchRoomName, true, true, queueName, true, new Dictionary<string, string>(), new Dictionary<string, string>(), ct);
                room = await SetupRoomTracking(ackTask, ct: ct);

                await room.MarkYourselfReady(gameEngineData, matchmakerData);
                await ResultUtils.WaitUntil(() => room.State.Users.All(x => x.IsReady), OperationTimeout, ct);

                await room.StartMatchmaking();
                await ResultUtils.WaitUntil(() => (room.State.MatchmakingData?.MatchmakingState).IsInsideMatchmaking(), OperationTimeout, ct);

                MatchmakingDataChanged += OnMatchmakingDataChanged;
                return room;
            }
            catch
            {
                if (room != null)
                {
                    await room.Leave();
                    room = null;
                }
                throw;
            }

            void OnMatchmakingDataChanged(MatchmakingDataChangedArgs args)
            {
                // ReSharper disable AccessToModifiedClosure
                if (room == null || room.IsDisposed)
                {
                    MatchmakingDataChanged -= OnMatchmakingDataChanged;
                    return;
                }
                if (args.RoomId != room.RoomId)
                    return;
                if ((room.State.MatchmakingData?.MatchmakingState).IsInsideMatchmakingOrMatch())
                    return;
                MatchmakingDataChanged -= OnMatchmakingDataChanged;
                room.Leave().Forget();
                // ReSharper restore AccessToModifiedClosure
            }
        }
        private async UniTask<IRoom> JoinRoom(string joinCode, uint? teamIndex)
        {
            var existingRoom = _rooms.Values.FirstOrDefault(x => x.State.JoinCode == joinCode);
            if (existingRoom?.IsJoined is true)
                throw new RoomAlreadyJoinedException(joinCode: joinCode);
            using var queueEntry = _joiningQueue.AddJoinCode(joinCode);
            return await SetupRoomTracking(_client.JoinRoom(joinCode, teamIndex));
        }

        private async UniTask<IRoom> JoinRoom(Guid roomId, uint? teamIndex)
        {
            if (_rooms.TryGetValue(roomId, out var room) && room.IsJoined)
                throw new RoomAlreadyJoinedException(roomId);
            using var queueEntry = _joiningQueue.AddRoomId(roomId);
            return await SetupRoomTracking(_client.JoinRoom(roomId, teamIndex), shouldSkipQueueCheck: true);
        }

        private async UniTask<IRoom> SetupRoomTracking(UniTask<Guid> mainOperation, bool shouldSkipQueueCheck = false, CancellationToken ct = default)
        {
            var roomId = await mainOperation;
            if (_rooms.TryGetValue(roomId, out var room) && room.IsJoined)
                throw new RoomAlreadyJoinedException(roomId);
            using var queueEntry = _joiningQueue.AddRoomId(roomId, shouldSkipQueueCheck);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await ResultUtils.WaitUntil(() => _rooms.TryGetValue(roomId, out var roomToJoin) && roomToJoin.IsJoined, OperationTimeout, linkedCts.Token);
            return _rooms[roomId];
        }
    }
}
