using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class Room : IRoom
    {
        public Guid RoomId => ThrowIfDisposedOrReturn(_roomId);
        public RoomState State => ThrowIfDisposedOrReturn(_state);

        public bool IsDisposed { get; private set; }

        public bool IsJoined
        {
            get => ((IRoom)this).IsJoined;
            internal set => ((IRoom)this).IsJoined = value;
        }

        bool IRoom.IsJoined
        {
            get => ThrowIfDisposedOrReturn(_isJoined);
            set
            {
                ThrowIfDisposed();
                _isJoined = value;
            }
        }

        public bool HasMatchmakingEnabled
        {
            get
            {
                ThrowIfDisposed();
                return _state.MatchmakingData == null;
            }
        }

        public bool IsMatchAvailable
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfNotJoined();
                ThrowIfNoMatchmaking();
                return _state.MatchmakingData is
                {
                    MatchmakingState: MatchmakingState.Playing,
                    MatchData: { State: MatchState.Running, MatchDetails: not null },
                };
            }
        }

        private readonly IMatchLauncher _matchLauncher;
        private readonly IRoomsClient _client;
        private readonly Guid _roomId;
        private readonly RoomState _state;
        private bool _isJoined;
        private bool _isCancellingInProgress;
        private readonly TimeSpan _forceCancelTimeout = TimeSpan.FromSeconds(10);
        private readonly bool _isEphemeral;
        private Guid LocalUserId => _client.SessionConnectionDetails.AuthData.UserId;
        private readonly TimeSpan _webApiTimeoutFallback = TimeSpan.FromSeconds(5);

        public Room(IMatchLauncher matchLauncher, IRoomsClient client, Guid roomId, RoomStateChanged initialState, bool isJoined = false)
        {
            _matchLauncher = matchLauncher;
            _client = client;
            _roomId = roomId;
            _state = new RoomState(initialState);
            _isJoined = isJoined;
            _isEphemeral = initialState.IsEphemeral;
        }

        public Room(IMatchLauncher matchLauncher, IRoomsClient client, Guid roomId, PublicRoomState initialState)
        {
            _matchLauncher = matchLauncher;
            _client = client;
            _roomId = roomId;
            _state = new RoomState(initialState);
        }

        void IRoom.UpdateState(RoomStateChanged roomState, in RoomStateDiff stateDiff)
        {
            ThrowIfDisposed();
            _state.Update(roomState, stateDiff);
        }

        void IRoom.UpdateState(PublicRoomState roomState)
        {
            ThrowIfDisposed();
            _state.Update(roomState);
        }

        bool IRoom.IsQuickMatch => _state is { RoomName: RoomUtil.QuickMatchRoomName, IsPrivate: true } && _isEphemeral;

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }

        public UniTask ChangeTeam(uint? teamIndex)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            if (teamIndex.HasValue
                && teamIndex.Value >= _state.MatchmakingData!.TeamCount)
                throw new ArgumentOutOfRangeException(nameof(teamIndex), teamIndex, $"Chosen team index must be lesser than {_state.MatchmakingData.TeamCount} or null");
            ElympicsLogger.Log($"[{nameof(Room)}] Id:{_roomId}. Change team to {teamIndex}.");
            return _client.ChangeTeam(_roomId, teamIndex).ContinueWith(() => ResultUtils.WaitUntil(() => GetLocalUser().TeamIndex == teamIndex, _webApiTimeoutFallback));
        }

        public UniTask MarkYourselfReady(byte[]? gameEngineData = null, float[]? matchmakerData = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            gameEngineData ??= Array.Empty<byte>();
            matchmakerData ??= Array.Empty<float>();
            ElympicsLogger.Log($"[{nameof(Room)}] Id:{_roomId}. Mark yourself {LocalUserId} ready.");
            //TODO: potential edge case. When setting isReady to true, we can get acknowledge however, backend can change our readiness after that thus we will never get isReady == true
            return _client.SetReady(_roomId, gameEngineData, matchmakerData).ContinueWith(() => ResultUtils.WaitUntil(() => GetLocalUser().IsReady, _webApiTimeoutFallback));
        }

        private UserInfo GetLocalUser() => _state.Users.First(x => x.UserId == LocalUserId);

        public UniTask MarkYourselfUnready()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            ElympicsLogger.Log($"[{nameof(Room)}] Id:{_roomId}. Mark yourself {LocalUserId} unready.");
            return _client.SetUnready(_roomId).ContinueWith(() => ResultUtils.WaitUntil(() => GetLocalUser().IsReady is false, _webApiTimeoutFallback));
        }

        public UniTask StartMatchmaking()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            var isAnyoneNotReady = _state.Users.Any(x => !x.IsReady);
            if (isAnyoneNotReady)
                throw new RoomRequirementsException("Not all players are ready.");

            ElympicsLogger.Log($"[{nameof(Room)}] Start matchmaking for Room: {_roomId}");
            return _client.StartMatchmaking(_roomId, _state.Host.UserId);
        }

        public async UniTask CancelMatchmaking(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();

            if (_isCancellingInProgress)
                throw new MatchmakingException("Matchmaking cancellation has been already requested.");

            if (!IsInValidStateToCancel())
                throw new MatchmakingException($"Can't cancel matchmaking during {_state.MatchmakingData!.MatchmakingState} state.");

            while (true)
                try
                {
                    ct.ThrowIfCancellationRequested();
                    _isCancellingInProgress = true;
                    ElympicsLogger.Log($"[{nameof(Room)}] Id:{_roomId}. Cancel matchmaking.");
                    await _client.CancelMatchmaking(_roomId, ct);
                    return;
                }
                catch (LobbyOperationException e)
                {
                    if (e.Kind is not (ErrorKind.FailedToCancelMatchmakingWithTimeout or ErrorKind.FailedToUnlockAfterSuccessfulRemove))
                        throw;

                    ct.ThrowIfCancellationRequested();
                    await UniTask.Delay(_forceCancelTimeout, DelayType.Realtime, PlayerLoopTiming.Update, ct);
                }
                finally
                {
                    _isCancellingInProgress = false;
                }

            bool IsInValidStateToCancel() => _state.MatchmakingData!.MatchmakingState is MatchmakingState.RequestingMatchmaking or MatchmakingState.Matchmaking or MatchmakingState.CancellingMatchmaking;
        }

        public UniTask UpdateRoomParams(string? roomName = null, bool? isPrivate = null, IReadOnlyDictionary<string, string>? customRoomData = null, IReadOnlyDictionary<string, string>? customMatchmakingData = null)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNotPrivilegedHost();

            var roomNameIsTheSame = roomName == null || roomName == _state.RoomName;
            var isPrivateIsTheSame = isPrivate == null || isPrivate == _state.IsPrivate;
            var customRoomDataIsTheSame = customRoomData == null || customRoomData.IsTheSame(_state.CustomData);
            var customMatchmakingDataIsTheSame = customMatchmakingData == null || customMatchmakingData.IsTheSame(_state.MatchmakingData?.CustomData);
            var isSameAsCurrentState = roomNameIsTheSame && isPrivateIsTheSame && customRoomDataIsTheSame && customMatchmakingDataIsTheSame;

            if (isSameAsCurrentState)
            {
                ElympicsLogger.LogWarning("No change compared to current room parameters. No message will be sent.");
                return UniTask.CompletedTask;
            }

            var dictMemorySize = customRoomData.GetSizeInBytes();
            if (dictMemorySize > DictionaryExtensions.MaxDictMemorySize)
                throw new RoomDataMemoryException(roomName, dictMemorySize);
            dictMemorySize = customMatchmakingData.GetSizeInBytes();
            if (dictMemorySize > DictionaryExtensions.MaxDictMemorySize)
                throw new RoomDataMemoryException(roomName, dictMemorySize);

            var roomNameToSend = roomName != _state.RoomName ? roomName : null;
            var isPrivateToSend = isPrivate != _state.IsPrivate ? isPrivate : null;
            var customRoomDataToSend = !customRoomDataIsTheSame ? customRoomData : null;
            var customMatchmakingDataToSend = !customMatchmakingDataIsTheSame ? customMatchmakingData : null;
            ElympicsLogger.Log($"[{nameof(Room)}] Id:{_roomId}. Sending Room Params to update:{Environment.NewLine}" + $" NewRoomName: {roomNameToSend}{Environment.NewLine}" + $" isPrivate {isPrivateToSend}{Environment.NewLine}" + $" customRoomDataSize: {customRoomDataToSend?.Count}{Environment.NewLine}" + $" customMatchmakingDataSize: {customMatchmakingDataToSend?.Count}{Environment.NewLine}");
            return _client.UpdateRoomParams(_roomId, _state.Host.UserId, roomNameToSend, isPrivateToSend, customRoomDataToSend, customMatchmakingDataToSend);
        }

        public void PlayAvailableMatch()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            var matchmakingData = State.MatchmakingData!;
            if (matchmakingData.MatchmakingState is not MatchmakingState.Playing)
                throw new InvalidOperationException($"Can't play match outside {MatchmakingState.Playing} matchmaking state. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}.");
            var matchData = matchmakingData.MatchData ?? throw new InvalidOperationException("No match data available. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}.");
            if (matchData.State is not MatchState.Running)
                throw new InvalidOperationException($"Can't play match outside {MatchState.Running} match state. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}, current match state: {matchData.State}.");
            var matchDetails = matchData.MatchDetails ?? throw new InvalidOperationException("No match details available. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}, current match state: {matchData.State}.");
            ElympicsLogger.Log($"[{nameof(Room)}]Id:{_roomId}. Play available match {matchData.MatchId}");
            _matchLauncher.PlayMatch(new MatchmakingFinishedData(matchData.MatchId, matchDetails, matchmakingData.QueueName, _client.SessionConnectionDetails.RegionName));
        }

        public UniTask Leave()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            if (State.MatchmakingData?.MatchmakingState is MatchmakingState.Matched or MatchmakingState.Playing)
                throw new InvalidOperationException($"Can't leave room during {_state.MatchmakingData!.MatchmakingState} state.");
            ElympicsLogger.Log($"[{nameof(Room)}]Id:{_roomId}. Leave room.");
            return _client.LeaveRoom(_roomId);
        }

        private T ThrowIfDisposedOrReturn<T>(T val, [CallerMemberName] string methodName = "")
        {
            ThrowIfDisposed(methodName);
            return val;
        }

        private void ThrowIfDisposed([CallerMemberName] string methodName = "")
        {
            if (IsDisposed)
                throw new RoomDisposedException(methodName);
        }

        private void ThrowIfNotJoined([CallerMemberName] string methodName = "")
        {
            if (!IsJoined)
                throw new RoomNotJoinedException(methodName);
        }

        private void ThrowIfNoMatchmaking([CallerMemberName] string methodName = "")
        {
            if (_state.MatchmakingData == null)
                throw new MatchmakingException($"Cannot call {methodName} in a non-matchmaking room.");
        }

        private void ThrowIfNotPrivilegedHost()
        {
            if (!_state.PrivilegedHost)
                throw new RoomPrivilegeException($"Only privileged hosts can call {nameof(UpdateRoomParams)}.");
        }
    }
}
