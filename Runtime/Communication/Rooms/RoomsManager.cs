using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Communication.Utils;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Rooms.Models;
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
        public event Action<RoomBetAmountChangedArgs>? RoomBetAmountChanged;
        public event Action<RoomNameChangedArgs>? RoomNameChanged;

        public event Action<MatchmakingDataChangedArgs>? MatchmakingDataChanged;
        public event Action<MatchmakingStartedArgs>? MatchmakingStarted;
        public event Action<MatchmakingEndedArgs>? MatchmakingEnded;
        public event Action<MatchDataReceivedArgs>? MatchDataReceived;
        public event Action<CustomMatchmakingDataChangedArgs>? CustomMatchmakingDataChanged;

        #endregion Aggregated room events

        private readonly Dictionary<Guid, IRoom> _rooms = new();
        private CancellationTokenSource _cts = new();

        private readonly IRoomJoiner _roomJoiner;

        public IRoom? CurrentRoom
        {
            get => GetRoomByOptionalId(_roomJoiner.CurrentRoomId);
            private set
            {
                if (value == CurrentRoom)
                    return;
                if (value is null)
                {
                    CurrentRoom!.IsJoined = false;
                    _roomJoiner.CurrentRoomId = null;
                }
                else
                {
                    value.IsJoined = true;
                    _roomJoiner.CurrentRoomId = value.RoomId;
                }
            }
        }

        private IRoom? GetRoomByOptionalId(Guid? roomId) => roomId is not null ? _rooms[roomId.Value] : null;

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
        private readonly ElympicsLoggerContext _logger;

        public RoomsManager(IMatchLauncher matchLauncher, IRoomsClient roomsClient, ElympicsLoggerContext logger, IRoomJoiner? roomJoiner = null)
        {
            _matchLauncher = matchLauncher;
            _client = roomsClient;
            _logger = logger.WithContext(nameof(RoomsManager));
            _roomJoiner = roomJoiner ?? new RoomJoiner(_client);
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
                var (roomId, updatedState) = listedRoomChange;

                // when a room ceases to exist
                if (updatedState == null)
                {
                    if (CurrentRoom?.RoomId == roomId)
                        CurrentRoom = null;
                    if (_rooms.Remove(roomId, out var removedRoom))
                        removedRoom.Dispose();
                    continue;
                }

                if (_rooms.TryGetValue(roomId, out var existingRoom))
                {
                    if (CurrentRoom?.RoomId != roomId)
                        existingRoom.UpdateState(updatedState);

                    // when a room no longer lists the player among its users
                    if (CurrentRoom?.RoomId == roomId && !updatedState.ContainsUser(_client.SessionConnectionDetails.AuthData!.UserId))
                    {
                        existingRoom.UpdateState(updatedState);
                        CurrentRoom = null;
                    }
                }
                else
                    AddRoomToDictionary(CreateRoom(roomId, publicState: updatedState));
            }

            RoomListUpdated?.Invoke(new RoomListUpdatedArgs(roomListChanged.Changes.Select(change => change.RoomId).ToList()));
        }

        void IRoomsManager.Reset()
        {
            _cts.Cancel();
            _cts.Dispose();

            var clearedRooms = _rooms.ToArray();
            _rooms.Clear();
            foreach (var (_, room) in clearedRooms)
                room.Dispose();
            _roomJoiner.Reset();
            _client.ResetState();

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
                if (CurrentRoom?.RoomId != roomId)
                {
                    CurrentRoom = room;
                    SetStateDiffToInitializeState();
                }
            }
            else
            {
                var newRoom = CreateRoom(roomId, state: roomState);
                _ = logger.SetQueue(roomState.MatchmakingData?.QueueName)
                    .SetRoomId(roomState.RoomId.ToString());
                CurrentRoom = newRoom;
                AddRoomToDictionary(newRoom);
                SetStateDiffToInitializeState();
            }

            var matchDataArgsAvailable = _stateDiff.MatchDataArgs != null;
            var matchNotFound = _stateDiff.MatchDataArgs?.MatchData.MatchDetails == null && !string.IsNullOrEmpty(_stateDiff.MatchDataArgs?.MatchData.FailReason);
            var matchFoundSuccessfully = _stateDiff.MatchDataArgs?.MatchData.MatchDetails != null && string.IsNullOrEmpty(_stateDiff.MatchDataArgs.MatchData.FailReason);
            if (matchDataArgsAvailable)
                _ = logger.SetMatchId(roomState.MatchmakingData?.MatchData?.MatchId.ToString());

            if (matchFoundSuccessfully || matchNotFound)
                _matchLauncher.MatchmakingCompleted();

            if (matchNotFound)
                logger.Log($"Match not found. Reason: {_stateDiff.MatchDataArgs?.MatchData.FailReason}");

            if (_initialized)
                InvokeEventsBasedOnStateDiff(roomId, _stateDiff);

            if (matchFoundSuccessfully)
            {
                logger.SetServerAddress(roomState.MatchmakingData?.MatchData?.MatchDetails?.TcpUdpServerAddress, roomState.MatchmakingData?.MatchData?.MatchDetails?.WebServerAddress)
                    .Log("Matchmaking completed successfully.");
                PlayAvailableMatchIfApplicable(roomId);
            }
            return;

            void SetStateDiffToInitializeState()
            {
                _stateDiff.Reset();
                _stateDiff.UpdatedState = true;
                _stateDiff.InitializedState = true;
            }
        }

        private async void HandleGameDataResponse(GameDataResponse obj)
        {
            try
            {
                ElympicsLogger.Log($"Handle Game Data Response {Environment.NewLine}{obj}");
                _ = _logger.SetGameVersionId(obj.GameVersionId).SetFleetName(obj.FleetName);
                await ElympicsLobbyClient.Instance!.AssignRoomCoins(obj.CoinData);
            }
            catch (Exception exception)
            {
                _ = ElympicsLogger.LogException(exception);
                _ = _tcs?.TrySetException(exception);
            }

            try
            {
                _tcs ??= new UniTaskCompletionSource<GameDataResponse>();
                _ = _tcs.TrySetResult(obj);
            }
            catch (Exception exception)
            {
                _ = ElympicsLogger.LogException(exception);
            }
        }

        private IRoom CreateRoom(Guid id, PublicRoomState? publicState = null, RoomStateChanged? state = null)
        {
            if (state is null && publicState is null)
                throw new ArgumentNullException($"One of the following arguments must be not null: {nameof(publicState)}, {nameof(state)}");
            return state is not null
                ? new Room(_matchLauncher, _client, id, state, false, _logger)
                : new Room(_matchLauncher, _client, id, publicState!, _logger);
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
            if (stateDiff.UpdatedBetAmount)
                RoomBetAmountChanged?.Invoke(new RoomBetAmountChangedArgs(roomId,
                    stateDiff.NewBetAmount == null ? null : new RoomBetAmount
                    {
                        CoinId = stateDiff.NewBetAmount.Value.CoinId,
                        BetValue = stateDiff.NewBetAmount.Value.BetAmount,
                    }));
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

        // TODO: shouldn't .IsJoined be a condition for this?
        private void PlayAvailableMatchIfApplicable(Guid roomId)
        {
            if (_matchLauncher is { ShouldLoadGameplaySceneAfterMatchmaking: true, IsCurrentlyInMatch: false })
                _rooms[roomId].PlayAvailableMatch();
        }

        private void HandleLeftRoom(LeftRoomArgs args)
        {
            if (CurrentRoom?.RoomId == args.RoomId)
                CurrentRoom = null;

            _ = _logger.SetNoRoom();
            if (args.Reason == LeavingReason.RoomClosed && _rooms.Remove(args.RoomId, out var removedRoom))
                removedRoom.Dispose();
            LeftRoom?.Invoke(args);
        }

        public bool TryGetAvailableRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (CurrentRoom?.RoomId != roomId && _rooms.TryGetValue(roomId, out var availableRoom))
                room = availableRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListAvailableRooms() => _rooms.Values.Where(room => CurrentRoom?.RoomId != room.RoomId).ToList();

        public bool TryGetJoinedRoom(Guid roomId, out IRoom? room)
        {
            room = null;
            if (CurrentRoom?.RoomId == roomId)
                room = CurrentRoom;
            return room is not null;
        }

        public IReadOnlyList<IRoom> ListJoinedRooms() => CurrentRoom is not null
            ? new[] { CurrentRoom }
            : new IRoom[] { };

        public UniTask StartTrackingAvailableRooms() => _client.WatchRooms();

        public async UniTask StopTrackingAvailableRooms()
        {
            await _client.UnwatchRooms();
            var clearedRooms = _rooms.Where(kvp => kvp.Key != CurrentRoom?.RoomId).ToArray();
            var currentRoom = CurrentRoom;
            _rooms.Clear();
            if (currentRoom is not null)
                _rooms.Add(currentRoom.RoomId, currentRoom);
            foreach (var (_, room) in clearedRooms)
                room.Dispose();
        }

        public bool IsTrackingAvailableRooms => _client.IsWatchingRooms;

        public UniTask<IRoom> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? tournamentDetails = null)
        {
            if (roomName == null)
                throw new ArgumentNullException(nameof(roomName));
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));
            customRoomData ??= new Dictionary<string, string>();
            customMatchmakingData ??= new Dictionary<string, string>();
            return _roomJoiner.CreateAndJoinRoom(
                roomName,
                queueName,
                isSingleTeam,
                isPrivate,
                isEphemeral: false,
                customRoomData,
                customMatchmakingData,
                tournamentDetails).ContinueWith(id => _rooms[id]);
        }

        public async UniTask<IRoom> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null)
        {
            if (roomId == null && joinCode == null)
                throw new ArgumentException($"{nameof(roomId)} and {nameof(joinCode)} cannot be null at the same time");

            var id = await _roomJoiner.JoinRoom(roomId, joinCode, teamIndex);

            if (!_rooms.TryGetValue(id, out var room))
                throw new InvalidOperationException("Room no longer exists.");

            return room;
        }

        public async UniTask<IRoom> StartQuickMatch(
            string queueName,
            byte[]? gameEngineData = null,
            float[]? matchmakerData = null,
            Dictionary<string, string>? customRoomData = null,
            Dictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? competitivenessConfig = null,
            CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));
            gameEngineData ??= Array.Empty<byte>();
            matchmakerData ??= Array.Empty<float>();
            customRoomData ??= new Dictionary<string, string>();
            customMatchmakingData ??= new Dictionary<string, string>();
            ct.ThrowIfCancellationRequested();

            bool isCancelled;
            var roomId = await _roomJoiner.CreateAndJoinRoom(RoomUtil.QuickMatchRoomName, queueName, true, true, true, customRoomData, customMatchmakingData, competitivenessConfig);
            using var roomLeftCts = new CancellationTokenSource();
            _client.LeftRoom += OnQuickRoomLeft;
            _ = logger.SetRoomId(roomId.ToString());
            _ = logger.SetQueue(queueName);

            var room = _rooms[roomId];
            var matchmakingCancelledCt = UniTask
                .WaitUntil(() => room.IsDisposed || room.State.MatchmakingData?.MatchmakingState is MatchmakingState.CancellingMatchmaking,
                    cancellationToken: roomLeftCts.Token)
                .ToCancellationToken();
            var matchmakingFailedCt = UniTask
                .WaitUntil(() => !room.IsDisposed && room.State.MatchmakingData is { MatchData: { FailReason: not null } },
                    cancellationToken: roomLeftCts.Token)
                .ToCancellationToken();
            var userCancelRequested = UniTask.WaitUntil(() => !room.IsDisposed && room.CanMatchmakingBeCancelled() && ct.IsCancellationRequested,
                cancellationToken: roomLeftCts.Token).ToCancellationToken();
            try
            {
                await SetupQuickRoomAndStartMatchmaking(gameEngineData, matchmakerData, room, ct);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(userCancelRequested, roomLeftCts.Token, matchmakingCancelledCt, matchmakingFailedCt);
                isCancelled = await UniTask.WaitUntil(() => !room.IsDisposed && room.State.MatchmakingData?.MatchData?.MatchDetails is not null,
                    cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
                _client.LeftRoom -= OnQuickRoomLeft;
                roomLeftCts.Cancel();
            }
            catch (Exception e)
            {
                await LeaveAndCleanUp();
                throw logger.CaptureAndThrow(e);
            }

            if (matchmakingFailedCt.IsCancellationRequested)
            {
                var error = room.State.MatchmakingData?.MatchData?.FailReason;
                await LeaveAndCleanUp();
                throw logger.CaptureAndThrow(new LobbyOperationException($"Failed to create quick match room. Error: {error}"));
            }

            // happy path
            if (!isCancelled)
                return room;

            // the process has been cancelled
            var timeoutCts = new CancellationTokenSource();
            using var timeoutDisposable = timeoutCts.CancelAfterSlim(ElympicsTimeout.RoomStateChangeConfirmationTimeout);
            try
            {
                if (userCancelRequested.IsCancellationRequested && !matchmakingCancelledCt.IsCancellationRequested)
                    await room.CancelMatchmaking(timeoutCts.Token);
            }
            catch (LobbyOperationException e)
            {
                logger.Warning($"Could not cancel quick match room matchmaking. Reason: {e.Message}");
                if (e.Kind == ErrorKind.RoomAlreadyInMatchedState)
                {
                    var timedOut = await UniTask.WaitUntil(() => !room.IsDisposed && room.IsEligibleToPlayMatch(), cancellationToken: timeoutCts.Token).SuppressCancellationThrow();
                    if (timedOut)
                        throw new ConfirmationTimeoutException(ElympicsTimeout.RoomStateChangeConfirmationTimeout);
                    return room;
                }

                if (!room.IsDisposed && room.IsEligibleToPlayMatch())
                    return room;
            }
            catch (OperationCanceledException)
            {
                throw new ConfirmationTimeoutException(ElympicsTimeout.RoomStateChangeConfirmationTimeout);
            }

            await LeaveAndCleanUp();

            throw new OperationCanceledException(ct);

            void OnQuickRoomLeft(LeftRoomArgs args)
            {
                if (!_rooms.TryGetValue(roomId, out var qmRoom) || qmRoom.IsDisposed)
                {
                    _client.LeftRoom -= OnQuickRoomLeft;
                    return;
                }
                _ = _logger.SetNoRoom();
                if (args.RoomId != roomId)
                    return;
                _client.LeftRoom -= OnQuickRoomLeft;
                roomLeftCts.Cancel();
            }

            async UniTask LeaveAndCleanUp()
            {
                if (room is { IsDisposed: false } && room == CurrentRoom)
                    await room.Leave();
                _ = logger.SetNoRoom();
            }
        }

        private static async UniTask SetupQuickRoomAndStartMatchmaking(byte[] gameEngineData, float[] matchmakerData, IRoom room, CancellationToken ct = default)
        {
            await room.ChangeTeam(0);
            await room.MarkYourselfReady(gameEngineData, matchmakerData, ct);
            await room.StartMatchmaking();
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
                var result = await _tcs.WithTimeout(ElympicsTimeout.FetchGameDataTimeout, _cts.Token);
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

                if (CurrentRoom?.IsMatchRoom() is true)
                    counter++;
                var canceled = await ResultUtils.WaitUntil(() => counter >= matchRoomsJoined, ElympicsTimeout.FetchGameDataTimeout, _cts.Token).SuppressCancellationThrow();
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
