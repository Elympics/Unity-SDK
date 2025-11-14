using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using Elympics.Communication.Mappers;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Communication.Rooms.InternalModels.ToRooms;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Util;

#nullable enable

namespace Elympics
{
    internal class RoomsClient : IRoomsClient
    {
        private readonly ElympicsLoggerContext _logger;
        public event Action<RoomStateChangedDto>? RoomStateChanged;
        public event Action<LeftRoomArgs>? LeftRoom;
        public event Action<RoomListChangedDto>? RoomListChanged;

        public RoomsClient(ElympicsLoggerContext logger) => _logger = logger.WithContext(nameof(RoomsClient));

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
                case RoomStateChangedDto stateChanged:
                    RoomStateChanged?.Invoke(stateChanged);
                    return;
                case RoomWasLeftDto roomLeft:
                    LeftRoom?.Invoke(new LeftRoomArgs(roomLeft.RoomId, roomLeft.Reason.Map()));
                    return;
                case RoomListChangedDto roomListChanged:
                    RoomListChanged?.Invoke(roomListChanged);
                    return;
                default:
                    return;
            }
        }

        public async UniTask<Guid> CreateRoom(
            string roomName,
            bool isPrivate,
            bool isEphemeral,
            string queueName,
            bool isSingleTeam,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            IReadOnlyDictionary<string, string>? customPlayerData = null,
            CompetitivenessConfig? competitivenessConfig = null,
            CancellationToken ct = default)
        {
            RoomBetDetailsSlim? betSlim = null;
            Guid? rollingTournamentBetConfigId = null;

            if (competitivenessConfig != null)
                switch (competitivenessConfig.CompetitivenessType)
                {
                    case CompetitivenessType.GlobalTournament:
                        customMatchmakingData = new Dictionary<string, string>(customMatchmakingData)
                        {
                            [TournamentConst.TournamentIdKey] = competitivenessConfig.ID
                        };
                        break;
                    case CompetitivenessType.RollingTournament:
                        rollingTournamentBetConfigId =
                            await RollingTournamentBetConfigIDs.GetConfigId(
                                Guid.Parse(competitivenessConfig.ID),
                                competitivenessConfig.Value,
                                competitivenessConfig.NumberOfPlayers,
                                competitivenessConfig.PrizeDistribution,
                                ct);
                        break;
                    case CompetitivenessType.Bet:
                        betSlim = GetRoomBetDetailsSlim(competitivenessConfig.Value, null, Guid.Parse(competitivenessConfig.ID));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(competitivenessConfig.CompetitivenessType), competitivenessConfig.CompetitivenessType, "Unexpected competitiveness type.");
                }

            _logger.WithMethodName().Log($"Create room {roomName}");
            return await ExecuteOperation<RoomOperationResultDto>(
                    new CreateRoomDto(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData, null, betSlim, rollingTournamentBetConfigId, customPlayerData),
                    ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex, IReadOnlyDictionary<string, string>? customPlayerData = null, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log($"Join room {roomId}");
            return ExecuteOperation<RoomOperationResultDto>(new JoinWithRoomIdDto(roomId, teamIndex, customPlayerData), ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex, IReadOnlyDictionary<string, string>? customPlayerData = null, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Join room using join code.");
            return ExecuteOperation<RoomOperationResultDto>(new JoinWithJoinCodeDto(joinCode, teamIndex, customPlayerData), ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask ChangeTeam(Guid roomId, uint? teamIndex, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log($"Set new team {teamIndex}.");
            return ExecuteOperation(new ChangeTeamDto(roomId, teamIndex), ct);
        }

        public UniTask SetReady(Guid roomId, byte[] gameEngineData, float[] matchmakerData, DateTime lastRoomUpdate, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Set ready.");
            return ExecuteOperation(new SetReadyDto(roomId, gameEngineData, matchmakerData, lastRoomUpdate), ct);
        }

        public UniTask SetUnready(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Set unready.");
            return ExecuteOperation(new SetUnreadyDto(roomId), ct);
        }

        public UniTask LeaveRoom(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Leave room.");
            return ExecuteOperation(new LeaveRoomDto(roomId), ct);
        }

        public async UniTask UpdateRoomParams(
            Guid roomId,
            Guid hostId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData,
            CompetitivenessConfig? competitivenessConfig = null,
            CancellationToken ct = default)
        {
            var betDetails = await FetchRoomBetDetailsSlim(competitivenessConfig, ct);
            await ExecuteOperationHostOnly(hostId, new SetRoomParametersDto(roomId, roomName, isPrivate, customRoomData, customMatchmakingData, null, betDetails), ct);
        }

        public UniTask UpdateCustomPlayerData(Guid roomId, Dictionary<string, string> customPlayerData, CancellationToken ct = default) => ExecuteOperation(new UpdateCustomPlayerDataDto(roomId, customPlayerData), ct);

        private static async UniTask<RoomBetDetailsSlim?> FetchRoomBetDetailsSlim(CompetitivenessConfig? config, CancellationToken ct)
        {
            if (config == null)
                return null;

            switch (config.CompetitivenessType)
            {
                case CompetitivenessType.GlobalTournament:
                    throw new ArgumentException($"Can't update competitiveness configuration for competitiveness type {config.CompetitivenessType}.");
                case CompetitivenessType.RollingTournament:
                {
                    var rollingTournamentBetConfigId = await RollingTournamentBetConfigIDs.GetConfigId(Guid.Parse(config.ID), config.Value, config.NumberOfPlayers, config.PrizeDistribution, ct);
                    return GetRoomBetDetailsSlim(config.Value, rollingTournamentBetConfigId, Guid.Parse(config.ID));
                }
                case CompetitivenessType.Bet:
                    return GetRoomBetDetailsSlim(config.Value, null, Guid.Parse(config.ID));
                default:
                    throw new ArgumentOutOfRangeException(nameof(config.CompetitivenessType), config.CompetitivenessType, "Unexpected competitiveness type.");
            }
        }

        public UniTask StartMatchmaking(Guid roomId, Guid hostId)
        {
            _logger.WithMethodName().Log("Start matchmaking.");
            return ExecuteOperationHostOnly(hostId, new StartMatchmakingDto(roomId));
        }

        public UniTask CancelMatchmaking(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Cancel matchmaking.");
            return ExecuteOperation(new CancelMatchmakingDto(roomId), ct);
        }

        private enum RoomWatchingState
        {
            NotWatching = 0,
            Watching,
            WatchRequestSent,
            UnwatchRequestSent,
        }

        private RoomWatchingState _roomWatchingState = RoomWatchingState.NotWatching;
        public bool IsWatchingRooms => _roomWatchingState is RoomWatchingState.Watching;

        public async UniTask WatchRooms(CancellationToken ct = default)
        {
            if (_roomWatchingState == RoomWatchingState.Watching)
                return;
            if (_roomWatchingState == RoomWatchingState.WatchRequestSent)
            {
                await UniTask.WaitUntil(() => _roomWatchingState == RoomWatchingState.Watching, cancellationToken: ct);
                return;
            }
            if (_roomWatchingState == RoomWatchingState.UnwatchRequestSent)
                throw new InvalidOperationException($"Cannot request watching rooms in {_roomWatchingState} state");

            _roomWatchingState = RoomWatchingState.WatchRequestSent;
            try
            {
                await ExecuteOperation(new WatchRoomsDto(), ct);
                _roomWatchingState = RoomWatchingState.Watching;
            }
            catch
            {
                _roomWatchingState = RoomWatchingState.NotWatching;
                throw;
            }
        }

        public async UniTask UnwatchRooms(CancellationToken ct = default)
        {
            if (_roomWatchingState == RoomWatchingState.NotWatching)
                return;
            if (_roomWatchingState == RoomWatchingState.UnwatchRequestSent)
            {
                await UniTask.WaitUntil(() => _roomWatchingState == RoomWatchingState.NotWatching, cancellationToken: ct);
                return;
            }
            if (_roomWatchingState == RoomWatchingState.WatchRequestSent)
                throw new InvalidOperationException($"Cannot request unwatching rooms in {_roomWatchingState} state");

            _roomWatchingState = RoomWatchingState.UnwatchRequestSent;
            try
            {
                await ExecuteOperation(new UnwatchRoomsDto(), ct);
                _roomWatchingState = RoomWatchingState.NotWatching;
            }
            catch
            {
                _roomWatchingState = RoomWatchingState.Watching;
                throw;
            }
        }

        private UniTask ExecuteOperation(LobbyOperation message, CancellationToken ct) =>
            ExecuteOperation<OperationResultDto>(message, ct);

        private async UniTask<T> ExecuteOperation<T>(LobbyOperation message, CancellationToken ct)
            where T : OperationResultDto
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            ct.ThrowIfCancellationRequested();
            var result = await Session.ExecuteOperation(message, ct);
            if (result is T expectedResult)
                return expectedResult;
            throw new UnexpectedRoomResultException(typeof(T), result.GetType());
        }

        private UniTask ExecuteOperationHostOnly(Guid? roomHostId, LobbyOperation message, CancellationToken ct = default, [CallerMemberName] string methodName = "")
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            if (roomHostId == null)
                throw new RoomPrivilegeException($"Cannot call ${methodName} on rooms without host.");
            var expectedUserId = SessionConnectionDetails.AuthData!.UserId;
            if (expectedUserId != null && roomHostId != expectedUserId)
                throw new RoomPrivilegeException($"Only hosts can call ${methodName} method.");

            return Session.ExecuteOperation(message, ct);
        }

        private static RoomBetDetailsSlim GetRoomBetDetailsSlim(decimal betValue, Guid? rollingBetId, Guid coinId)
        {
            var coinDecimal = ElympicsLobbyClient.Instance!.FetchDecimalForCoin(coinId) ?? throw new ArgumentException($"Couldn't create bet with coinId: {coinId}");
            return new RoomBetDetailsSlim(RawCoinConverter.ToRaw(betValue, coinDecimal), coinId, rollingBetId);
        }
        void IRoomsClient.ResetState() => _roomWatchingState = RoomWatchingState.NotWatching;
        void IRoomsClient.ClearSession() => Session = null;
    }
}
