using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Rooms.Models;
using JetBrains.Annotations;
using MatchmakingState = Elympics.Rooms.Models.MatchmakingState;

#nullable enable

namespace Elympics
{
    internal class RoomsManager : IRoomsManager
    {
        #region Aggregated room events

        public event Action<RoomListUpdatedArgs>? RoomListUpdated;
        public event Action<JoinedRoomUpdatedArgs>? JoinedRoomUpdated;

        [Obsolete("This event will be not supported in the future.")]
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

        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _quickJoinTimeout = TimeSpan.FromSeconds(10);

        private readonly Dictionary<Guid, IRoom> _rooms = new();
        private readonly IRoomJoiningQueue _joiningQueue;
        private CancellationTokenSource _cts = new();

        private readonly IMatchLauncher _matchLauncher;
        private readonly IRoomsClient _client;

        private readonly RoomStateDiff _stateDiff = new();

        private UniTaskCompletionSource<GameDataResponse>? _tcs;

        public event Func<IRoom, IRoom>? RoomSetUp
        {
            add
            {
                if (value != null
                    && _roomDecorators.IndexOf(value) == -1)
                    _roomDecorators.Add(value);
            }
            remove
            {
                if (value != null)
                    _ = _roomDecorators.RemoveAll(x => x == value);
            }
        }

        private readonly List<Func<IRoom, IRoom>> _roomDecorators = new();
        private bool _initialized;
        private ElympicsLoggerContext _logger;

        public RoomsManager(IMatchLauncher matchLauncher, IRoomsClient roomsClient, ElympicsLoggerContext logger, IRoomJoiningQueue? joiningQueue = null)
        {
            _matchLauncher = matchLauncher;
            _client = roomsClient;
            _joiningQueue = joiningQueue ?? new RoomJoiningQueue();
            _logger = logger.WithContext(nameof(RoomsManager));
            SubscribeClient();
        }

        private void SubscribeClient()
        {
            _client.GameDataResponse += HandleGameDataResponse;
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
                    else if (existingRoom.IsJoined
                             && stateUpdate.RoomContainUser(_client.SessionConnectionDetails.AuthData!.UserId) is false)
                    {
                        existingRoom.UpdateState(stateUpdate);
                        existingRoom.ToggleJoinStatus(false);
                    }
                }
                else
                    AddRoomToDictionary(new Room(_matchLauncher, _client, roomId, stateUpdate));
            }
            RoomListUpdated?.Invoke(new RoomListUpdatedArgs(roomListChanged.Changes.Select(change => change.RoomId).ToList()));
        }

        void IRoomsManager.Reset()
        {
            _cts.Cancel();
            _cts.Dispose();
            var clearedRooms = _rooms.ToArray();
            _rooms.Clear();
            _joiningQueue.Clear();
            foreach (var (roomId, room) in clearedRooms)
            {
                try
                {
                    if (room.IsJoined)
                    {
                        room.Dispose();
                        LeftRoom?.Invoke(new LeftRoomArgs(roomId, LeavingReason.RoomClosed));
                    }
                    else
                    {
                        room.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _ = ElympicsLogger.LogException(ex);
                }
            }
            _cts = new CancellationTokenSource();
            _initialized = false;
        }

        private void HandleJoinedRoomUpdated(RoomStateChanged roomState)
        {
            var logger = _logger.WithMethodName();
            logger.Log($"Handle room update.{Environment.NewLine}{roomState}");
            var roomId = roomState.RoomId;
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.UpdateState(roomState, _stateDiff);
                if (!room.IsJoined)
                {
                    room.ToggleJoinStatus(true);
                    _stateDiff.InitializedState = true;
                }
            }
            else
            {
                IRoom newRoom = new Room(_matchLauncher, _client, roomId, roomState);
                _ = logger.SetRegion(roomState.MatchmakingData?.QueueName).SetRoomId(roomState.RoomId.ToString());
                newRoom.ToggleJoinStatus(true);
                AddRoomToDictionary(newRoom);
                SetStateDiffToInitializeState();
            }

            var mmCompleted = _stateDiff.MatchDataArgs != null;
            var matchFound = _stateDiff.MatchDataArgs != null && string.IsNullOrEmpty(_stateDiff.MatchDataArgs.MatchData.FailReason);
            if (mmCompleted)
            {
                _ = _logger.SetMatchId(roomState.MatchmakingData?.MatchData?.MatchId.ToString());
                _ = _logger.SetServerAddress(roomState.MatchmakingData?.MatchData?.MatchDetails?.TcpUdpServerAddress, roomState.MatchmakingData?.MatchData?.MatchDetails?.WebServerAddress);
                _matchLauncher.MatchFound();
            }

            if (_initialized)
                InvokeEventsBasedOnStateDiff(roomId, _stateDiff);

            PlayAvailableMatchIfApplicable(roomId, matchFound);
            return;

            void SetStateDiffToInitializeState()
            {
                _stateDiff.Reset();
                _stateDiff.UpdatedState = true;
                _stateDiff.InitializedState = true;
            }
        }

        private void HandleGameDataResponse(GameDataResponse obj)
        {
            ElympicsLogger.Log($"Handle Game Data Response {Environment.NewLine}{obj}");
            _tcs ??= new UniTaskCompletionSource<GameDataResponse>();
            _ = _tcs.TrySetResult(obj);
        }

        private void AddRoomToDictionary(IRoom room)
        {
            foreach (var decorator in _roomDecorators)
                room = decorator(room);
            _rooms.Add(room.RoomId, room);
        }

        private void InvokeEventsBasedOnStateDiff(Guid roomId, RoomStateDiff stateDiff)
        {
            if (!stateDiff.UpdatedState)
                return;
            JoinedRoomUpdated?.Invoke(new JoinedRoomUpdatedArgs(roomId));
            if (stateDiff.InitializedState)
            {
                JoinedRoom?.Invoke(new JoinedRoomArgs(roomId));
                return;
            }
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

        private void PlayAvailableMatchIfApplicable(Guid roomId, bool matchFound)
        {
            if (matchFound is false)
                return;
            if (_matchLauncher is { ShouldLoadGameplaySceneAfterMatchmaking: true, IsCurrentlyInMatch: false })
                _rooms[roomId].PlayAvailableMatch();
        }

        private void HandleLeftRoom(LeftRoomArgs args)
        {
            if (_rooms.TryGetValue(args.RoomId, out var room))
                room.ToggleJoinStatus(false);
            _ = _logger.SetNoRoom();
            LeftRoom?.Invoke(args);
        }

        public bool TryGetAvailableRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (_rooms.TryGetValue(roomId, out var availableRoom)
                && !availableRoom.IsJoined)
                room = availableRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListAvailableRooms() => _rooms.Values.Where(room => !room.IsJoined).ToList();

        public bool TryGetJoinedRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (_rooms.TryGetValue(roomId, out var joinedRoom)
                && joinedRoom.IsJoined)
                room = joinedRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListJoinedRooms() => _rooms.Values.Where(room => room.IsJoined).ToList();

        public UniTask StartTrackingAvailableRooms() => _client.WatchRooms();

        public UniTask StopTrackingAvailableRooms() => _client.UnwatchRooms();

        #region Public API

        [PublicAPI]
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
        [PublicAPI]
        public UniTask<IRoom> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null)
        {
            if (joinCode != null)
                return JoinRoom(joinCode, teamIndex);
            if (roomId != null)
                return JoinRoom(roomId.Value, teamIndex);
            throw new ArgumentException($"{nameof(roomId)} and {nameof(joinCode)} cannot be null at the same time");
        }
        [PublicAPI]
        public async UniTask<IRoom> StartQuickMatch(
            string queueName,
            byte[]? gameEngineData = null,
            float[]? matchmakerData = null,
            Dictionary<string, string>? customRoomData = null,
            Dictionary<string, string>? customMatchmakingData = null,
            CancellationToken ct = default)
        {
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));
            gameEngineData ??= Array.Empty<byte>();
            matchmakerData ??= Array.Empty<float>();
            ct.ThrowIfCancellationRequested();

            var logger = _logger.WithMethodName();
            IRoom? room = null;
            try
            {
                var ackTask = _client.CreateRoom(RoomUtil.QuickMatchRoomName,
                    true,
                    true,
                    queueName,
                    true,
                    customRoomData ?? new Dictionary<string, string>(),
                    customMatchmakingData ?? new Dictionary<string, string>(),
                    ct);
                room = await SetupRoomTracking(ackTask, ct: ct);

                await room.ChangeTeam(0);
                await room.MarkYourselfReady(gameEngineData, matchmakerData);

                await room.StartMatchmaking();
                _client.LeftRoom += OnQuickRoomLeft;

                var isCanceled = await UniTask.WaitUntil(() => _stateDiff.MatchDataArgs is not null, cancellationToken: ct).SuppressCancellationThrow();

                if (isCanceled is false)
                {
                    var error = room.State.MatchmakingData?.MatchData?.FailReason;
                    if (!string.IsNullOrEmpty(error))
                        throw logger.CaptureAndThrow(new LobbyOperationException($"Failed to create quick match room. Error: {error}"));

                    logger.Log("Quick Match Founded.");
                    return room;
                }

                try
                {
                    await room.CancelMatchmaking();
                    await room.Leave();
                }
                catch (Exception e)
                {
                    if (room.State.MatchmakingData?.MatchmakingState == MatchmakingState.Unlocked)
                    {
                        await room.Leave();
                        _ = logger.SetNoRoom();
                    }
                    else if (room.IsEligibleToPlayMatch())
                        return room;
                    else
                    {
                        _ = logger.SetNoRoom();
                        throw logger.CaptureAndThrow(e);
                    }
                }
                return room;
            }
            catch (Exception e)
            {
                if (room != null && !room.IsDisposed && room.IsJoined)
                    await room.Leave();

                room = null;
                _ = logger.SetNoRoom();
                throw logger.CaptureAndThrow(e);
            }

            void OnQuickRoomLeft(LeftRoomArgs args)
            {
                _ = _logger.SetNoRoom();
                // ReSharper disable AccessToModifiedClosure
                if (room == null
                    || room.IsDisposed)
                {
                    _client.LeftRoom -= OnQuickRoomLeft;
                    return;
                }
                if (args.RoomId != room.RoomId)
                    return;
                // if ((room.State.MatchmakingData?.MatchmakingState).IsInsideMatchmakingOrMatch())
                // return;
                _client.LeftRoom -= OnQuickRoomLeft;
                if (_rooms.Remove(room.RoomId, out var removedRoom))
                    removedRoom.Dispose();
                // ReSharper restore AccessToModifiedClosure
            }
        }

        #endregion

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
            if (_rooms.TryGetValue(roomId, out var room)
                && room.IsJoined)
                throw new RoomAlreadyJoinedException(roomId);
            using var queueEntry = _joiningQueue.AddRoomId(roomId);
            return await SetupRoomTracking(_client.JoinRoom(roomId, teamIndex), shouldSkipQueueCheck: true);
        }

        private async UniTask<IRoom> SetupRoomTracking(UniTask<Guid> mainOperation, bool shouldSkipQueueCheck = false, CancellationToken ct = default)
        {
            var roomId = await mainOperation;
            if (_rooms.TryGetValue(roomId, out var room)
                && room.IsJoined)
                throw new RoomAlreadyJoinedException(roomId);
            using var queueEntry = _joiningQueue.AddRoomId(roomId, shouldSkipQueueCheck);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await ResultUtils.WaitUntil(() => _rooms.TryGetValue(roomId, out var roomToJoin) && roomToJoin.IsJoined, _operationTimeout, linkedCts.Token);
            return _rooms[roomId];
        }

        async UniTask IRoomsManager.CheckJoinedRoomStatus()
        {
            if (_initialized)
                return;

            var counter = 0;
            try
            {
                _client.RoomStateChanged += OnRoomStateChanged;
                _tcs ??= new UniTaskCompletionSource<GameDataResponse>();
                var result = await _tcs.WithTimeout(_operationTimeout, _cts.Token);
                if (result is null)
                {
                    _initialized = true;
                    return;
                }

                var matchRoomsJoined = result.JoinedMatchRooms;
                if (matchRoomsJoined <= 0)
                {
                    _initialized = true;
                    return;
                }

                var currentJoinedMatchRooms = _rooms.Count(pair => pair.Value.IsJoined && pair.Value.IsMatchRoom());
                counter += currentJoinedMatchRooms;
                var canceled = await ResultUtils.WaitUntil(() => counter >= matchRoomsJoined, _operationTimeout, _cts.Token).SuppressCancellationThrow();
                if (canceled)
                    ElympicsLogger.LogWarning("Waiting for init room state timeout.");

                _initialized = true;
            }
            finally
            {
                _client.RoomStateChanged -= OnRoomStateChanged;
                _tcs = null;
            }
            return;

            void OnRoomStateChanged(RoomStateChanged obj)
            {
                if (_rooms[obj.RoomId].IsMatchRoom())
                    // ReSharper disable once AccessToModifiedClosure
                    ++counter;
            }
        }
    }
}
