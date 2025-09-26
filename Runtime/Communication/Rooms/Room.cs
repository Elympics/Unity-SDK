using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Communication.Utils;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using MatchmakingState = Elympics.Rooms.Models.MatchmakingState;

#nullable enable

namespace Elympics
{
    internal class Room : IRoom
    {
        public Guid RoomId => ThrowIfDisposedOrReturn(_roomId);
        public RoomState State => ThrowIfDisposedOrReturn(_state);

        public bool IsDisposed { get; private set; }

        public bool IsJoined => ThrowIfDisposedOrReturn(_isJoined);

        /// <summary>Awaited before operation is sent to backend by <see cref="MarkYourselfReady"/>.</summary>
        /// <remarks>This can be used to perform additional operations required before client can be set as ready.</remarks>
        public static Func<Room, CancellationToken, UniTask>? BeforeMarkYourselfReady;

        bool IRoom.IsJoined
        {
            get => IsJoined;
            set
            {
                if (_isJoined ^ value)
                {
                    if (value)
                        _roomStateChangeMonitorCts = new CancellationTokenSource();
                    else
                        _roomStateChangeMonitorCts.Cancel();
                }
                _isJoined = value;
            }
        }

        private bool _isJoined;

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
        private readonly ElympicsLoggerContext? _logger;
        private readonly RoomState _state;
        private readonly bool _isEphemeral;
        private Guid? LocalUserId => _client.SessionConnectionDetails.AuthData?.UserId;

        private CancellationTokenSource _roomStateChangeMonitorCts;

        public Room(
            IMatchLauncher matchLauncher,
            IRoomsClient client,
            Guid roomId,
            RoomStateChangedDto initialState,
            bool isJoined = false,
            ElympicsLoggerContext? logger = null) : this(matchLauncher, client, roomId, new RoomState(initialState), isJoined, initialState.IsEphemeral, logger)
        { }

        public Room(IMatchLauncher matchLauncher, IRoomsClient client, Guid roomId, PublicRoomState initialState, ElympicsLoggerContext? logger = null)
            : this(matchLauncher, client, roomId, new RoomState(initialState), logger: logger)
        { }

        private Room(
            IMatchLauncher matchLauncher,
            IRoomsClient client,
            Guid roomId,
            RoomState state,
            bool isJoined = false,
            bool isEphemeral = false,
            ElympicsLoggerContext? logger = null)
        {
            _matchLauncher = matchLauncher;
            _client = client;
            _roomId = roomId;
            _state = state;
            _roomStateChangeMonitorCts = new CancellationTokenSource();
            _roomStateChangeMonitorCts.Cancel();
            _isJoined = isJoined;
            _isEphemeral = isEphemeral;
            _logger = logger?.WithContext($"{nameof(Room)}");
        }

        void IRoom.UpdateState(RoomStateChangedDto roomState, in RoomStateDiff stateDiff)
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
            _roomStateChangeMonitorCts.Cancel();
        }

        public async UniTask ChangeTeam(uint? teamIndex)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            if (teamIndex.HasValue
                && teamIndex.Value >= _state.MatchmakingData!.TeamCount)
                throw new ArgumentOutOfRangeException(nameof(teamIndex), teamIndex, $"Chosen team index must be lesser than {_state.MatchmakingData.TeamCount} or null");

            await _client.ChangeTeam(_roomId, teamIndex, _roomStateChangeMonitorCts.Token);
            await ResultUtils.WaitUntil(() => !TryGetLocalUser(out var localUser) || localUser!.TeamIndex == teamIndex, ElympicsTimeout.RoomStateChangeConfirmationTimeout, _roomStateChangeMonitorCts.Token);
        }

        public async UniTask MarkYourselfReady(byte[]? gameEngineData = null, float[]? matchmakerData = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            gameEngineData ??= Array.Empty<byte>();
            matchmakerData ??= Array.Empty<float>();

            if (BeforeMarkYourselfReady != null)
                await BeforeMarkYourselfReady(this, ct);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _roomStateChangeMonitorCts.Token);
            // TODO: potential edge case. When setting isReady to true, we can get acknowledge however, backend can change our readiness after that thus we will never get isReady == true
            await _client.SetReady(_roomId, gameEngineData, matchmakerData, _state.LastRoomUpdate, ct);
            await ResultUtils.WaitUntil(() => !TryGetLocalUser(out var localUser) || localUser!.IsReady, ElympicsTimeout.RoomStateChangeConfirmationTimeout, linkedCts.Token);
        }

        private UserInfo GetLocalUser() => _state.Users.First(x => x.UserId == LocalUserId);

        private bool TryGetLocalUser(out UserInfo? localUser)
        {
            localUser = _state.Users.FirstOrDefault(x => x.UserId == LocalUserId);
            return localUser != null;
        }

        public async UniTask MarkYourselfUnready()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            await _client.SetUnready(_roomId, _roomStateChangeMonitorCts.Token);
            await ResultUtils.WaitUntil(() => !TryGetLocalUser(out var localUser) || !localUser!.IsReady, ElympicsTimeout.RoomStateChangeConfirmationTimeout, _roomStateChangeMonitorCts.Token);
        }

        public async UniTask StartMatchmaking()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            var isAnyoneNotReady = _state.Users.Any(x => !x.IsReady);
            if (isAnyoneNotReady)
                throw new RoomRequirementsException("Not all players are ready.");

            await _matchLauncher.StartMatchmaking(this);
            await WaitForState(() => _state.MatchmakingData!.MatchmakingState != MatchmakingState.Unlocked || _state.MatchmakingData.MatchData?.FailReason is not null, _roomStateChangeMonitorCts.Token);
        }

        UniTask IRoom.StartMatchmakingInternal()
        {
            _state.ResetMatchData();
            return _client.StartMatchmaking(_roomId, _state.Host.UserId);
        }

        public async UniTask CancelMatchmaking(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();

            if (!_state.MatchmakingData!.MatchmakingState.IsMatchMakingStateValidToCancel())
                throw new MatchmakingException($"Can't cancel matchmaking during {_state.MatchmakingData!.MatchmakingState} state.");
            await _matchLauncher.CancelMatchmaking(this, ct);
        }

        async UniTask IRoom.CancelMatchmakingInternal(CancellationToken ct)
        {
            while (true)
                try
                {
                    ct.ThrowIfCancellationRequested();
                    await _client.CancelMatchmaking(_roomId, ct);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _roomStateChangeMonitorCts.Token);
                    await WaitForState(() => IsDisposed || _state.MatchmakingData!.MatchmakingState == MatchmakingState.Unlocked, linked.Token);
                    return;
                }
                catch (LobbyOperationException e)
                {
                    if (e.Kind is not (ErrorKind.FailedToCancelMatchmakingWithTimeout or ErrorKind.FailedToUnlockAfterSuccessfulRemove))
                        throw;

                    ct.ThrowIfCancellationRequested();
                    await UniTask.Delay(ElympicsTimeout.ForceMatchmakingCancellationTimeout, DelayType.Realtime, PlayerLoopTiming.Update, ct);
                }
        }

        public UniTask UpdateRoomParams(
            string? roomName = null,
            bool? isPrivate = null,
            IReadOnlyDictionary<string, string>? customRoomData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? competitivenessConfig = null)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNotPrivilegedHost();

            var roomNameIsTheSame = roomName == null || roomName == _state.RoomName;
            var isPrivateIsTheSame = isPrivate == null || isPrivate == _state.IsPrivate;
            var customRoomDataIsTheSame = customRoomData == null || customRoomData.IsTheSame(_state.CustomData);
            var customMatchmakingDataIsTheSame = customMatchmakingData == null || customMatchmakingData.IsTheSame(_state.MatchmakingData?.CustomData);
            var isCompetitivenessConfigTheSame = competitivenessConfig == null || IsCompetitivenessConfigTheSame(competitivenessConfig);
            var isSameAsCurrentState = roomNameIsTheSame && isPrivateIsTheSame && customRoomDataIsTheSame && customMatchmakingDataIsTheSame && isCompetitivenessConfigTheSame;

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
            var competetivnessConfigToSend = isCompetitivenessConfigTheSame ? null : competitivenessConfig;
            return _client.UpdateRoomParams(_roomId, _state.Host.UserId, roomNameToSend, isPrivateToSend, customRoomDataToSend, customMatchmakingDataToSend, competetivnessConfigToSend);

            bool IsCompetitivenessConfigTheSame(CompetitivenessConfig config) =>
                config.CompetitivenessType switch
                {
                    CompetitivenessType.GlobalTournament => _state.MatchmakingData != null
                                                      && _state.MatchmakingData.CustomData.TryGetValue(TournamentConst.TournamentIdKey, out var tournamentId)
                                                      && tournamentId == competitivenessConfig.ID,
                    CompetitivenessType.RollingTournament => false, // TODO: Check this properly once information about rolling tournaments is included in room ~kdudziak 10.06.2025
                    CompetitivenessType.Bet => _state.MatchmakingData?.BetDetails is { } roomBetDetails
                                               && roomBetDetails.BetValue == config.Value
                                               && roomBetDetails.Coin.CoinId.ToString() == config.ID,
                    _ => throw new ArgumentOutOfRangeException()
                };
        }

        public async UniTask UpdateCustomPlayerData(Dictionary<string, string>? customPlayerData)
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            await _client.UpdateCustomPlayerData(RoomId, customPlayerData ?? new Dictionary<string, string>());
        }

        public void PlayAvailableMatch()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            ThrowIfNoMatchmaking();
            var matchmakingData = State.MatchmakingData!;
            if (!this.IsEligibleToPlayMatch())
                throw new InvalidOperationException($"Can't play match outside {MatchmakingState.Playing} matchmaking state. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}.");
            var matchData = matchmakingData.MatchData ?? throw new InvalidOperationException("No match data available. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}.");
            if (matchData.State is not MatchState.Running)
                throw new InvalidOperationException($"Can't play match outside {MatchState.Running} match state. "
                    + $"Current matchmaking state: {matchmakingData.MatchmakingState}, current match state: {matchData.State}.");
            var matchDetails = matchData.MatchDetails
                ?? throw new InvalidOperationException("No match details available. " + $"Current matchmaking state: {matchmakingData.MatchmakingState}, current match state: {matchData.State}.");
            _matchLauncher.PlayMatch(new MatchmakingFinishedData(matchData.MatchId, matchDetails, matchmakingData.QueueName, _client.SessionConnectionDetails.RegionName));
        }
        public async UniTask Leave()
        {
            ThrowIfDisposed();
            ThrowIfNotJoined();
            if (State.MatchmakingData?.MatchmakingState is MatchmakingState.Playing)
                throw new InvalidOperationException($"Can't leave room during {_state.MatchmakingData!.MatchmakingState} state.");
            await _client.LeaveRoom(_roomId);
            await WaitForState(() => IsDisposed || !_isJoined);
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

        private async UniTask WaitForState(Func<bool> predicate, CancellationToken ct = default, [CallerMemberName] string callerName = "")
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var _ = cts.CancelAfterSlim(ElympicsTimeout.RoomStateChangeConfirmationTimeout, DelayType.Realtime);
            if (await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, cts.Token).SuppressCancellationThrow())
            {
                ct.ThrowIfCancellationRequested();
                var logger = _logger?.WithMethodName();
                var exception = new TimeoutException($"Room state has not been updated in time after {callerName} has been issued");
                throw logger?.CaptureAndThrow(exception) ?? exception;
            }
        }
    }
}
