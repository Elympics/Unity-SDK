using System;
using Elympics.Communication.Lobby.InternalModels;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal static class RoomsMapper
    {
        public static ChainType Map(this ChainTypeDto dto) => dto switch
        {
            ChainTypeDto.TON => ChainType.TON,
            ChainTypeDto.EVM => ChainType.EVM,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static ErrorKind Map(this ErrorKindDto dto) => dto switch
        {
            ErrorKindDto.Unspecified => ErrorKind.Unspecified,
            ErrorKindDto.GameDoesNotExist => ErrorKind.GameDoesNotExist,
            ErrorKindDto.GameVersionDoesNotExist => ErrorKind.GameVersionDoesNotExist,
            ErrorKindDto.QueueDoesNotExist => ErrorKind.QueueDoesNotExist,
            ErrorKindDto.RegionDoesNotExist => ErrorKind.RegionDoesNotExist,
            ErrorKindDto.RoomDoesNotExist => ErrorKind.RoomDoesNotExist,
            ErrorKindDto.RoomLocked => ErrorKind.RoomLocked,
            ErrorKindDto.RoomFull => ErrorKind.RoomFull,
            ErrorKindDto.TeamFull => ErrorKind.TeamFull,
            ErrorKindDto.Outdated => ErrorKind.Outdated,
            ErrorKindDto.AlreadyInRoom => ErrorKind.AlreadyInRoom,
            ErrorKindDto.NotInRoom => ErrorKind.NotInRoom,
            ErrorKindDto.RoomAlreadyExists => ErrorKind.RoomAlreadyExists,
            ErrorKindDto.RoomWithoutMatchmaking => ErrorKind.RoomWithoutMatchmaking,
            ErrorKindDto.AlreadyReady => ErrorKind.AlreadyReady,
            ErrorKindDto.AlreadyNotReady => ErrorKind.AlreadyNotReady,
            ErrorKindDto.NotHost => ErrorKind.NotHost,
            ErrorKindDto.NotEveryoneReady => ErrorKind.NotEveryoneReady,
            ErrorKindDto.AlreadyInTeam => ErrorKind.AlreadyInTeam,
            ErrorKindDto.InvalidTeam => ErrorKind.InvalidTeam,
            ErrorKindDto.FailedToUnlockAfterSuccessfulRemove => ErrorKind.FailedToUnlockAfterSuccessfulRemove,
            ErrorKindDto.FailedToCancelMatchmakingWithTimeout => ErrorKind.FailedToCancelMatchmakingWithTimeout,
            ErrorKindDto.RoomPrivate => ErrorKind.RoomPrivate,
            ErrorKindDto.OnPlayerReadyRejected => ErrorKind.OnPlayerReadyRejected,
            ErrorKindDto.OnStartMatchmakingRejected => ErrorKind.OnStartMatchmakingRejected,
            ErrorKindDto.MatchNotFound => ErrorKind.MatchNotFound,
            ErrorKindDto.CoinDoesNotExist => ErrorKind.CoinDoesNotExist,
            ErrorKindDto.ConfigurationNotValid => ErrorKind.ConfigurationNotValid,
            ErrorKindDto.AcceptedRoomIsOutdated => ErrorKind.AcceptedRoomIsOutdated,
            ErrorKindDto.RoomAlreadyInMatchedState => ErrorKind.RoomAlreadyInMatchedState,
            ErrorKindDto.JoinCodeMismatch => ErrorKind.JoinCodeMismatch,
            ErrorKindDto.Throttle => ErrorKind.Throttle,
            ErrorKindDto.InvalidMessage => ErrorKind.InvalidMessage,
            ErrorKindDto.ValidationError => ErrorKind.ValidationError,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static ErrorKindDto Map(this ErrorKind entity) => entity switch
        {
            ErrorKind.Unspecified => ErrorKindDto.Unspecified,
            ErrorKind.GameDoesNotExist => ErrorKindDto.GameDoesNotExist,
            ErrorKind.GameVersionDoesNotExist => ErrorKindDto.GameVersionDoesNotExist,
            ErrorKind.QueueDoesNotExist => ErrorKindDto.QueueDoesNotExist,
            ErrorKind.RegionDoesNotExist => ErrorKindDto.RegionDoesNotExist,
            ErrorKind.RoomDoesNotExist => ErrorKindDto.RoomDoesNotExist,
            ErrorKind.RoomLocked => ErrorKindDto.RoomLocked,
            ErrorKind.RoomFull => ErrorKindDto.RoomFull,
            ErrorKind.TeamFull => ErrorKindDto.TeamFull,
            ErrorKind.Outdated => ErrorKindDto.Outdated,
            ErrorKind.AlreadyInRoom => ErrorKindDto.AlreadyInRoom,
            ErrorKind.NotInRoom => ErrorKindDto.NotInRoom,
            ErrorKind.RoomAlreadyExists => ErrorKindDto.RoomAlreadyExists,
            ErrorKind.RoomWithoutMatchmaking => ErrorKindDto.RoomWithoutMatchmaking,
            ErrorKind.AlreadyReady => ErrorKindDto.AlreadyReady,
            ErrorKind.AlreadyNotReady => ErrorKindDto.AlreadyNotReady,
            ErrorKind.NotHost => ErrorKindDto.NotHost,
            ErrorKind.NotEveryoneReady => ErrorKindDto.NotEveryoneReady,
            ErrorKind.AlreadyInTeam => ErrorKindDto.AlreadyInTeam,
            ErrorKind.InvalidTeam => ErrorKindDto.InvalidTeam,
            ErrorKind.FailedToUnlockAfterSuccessfulRemove => ErrorKindDto.FailedToUnlockAfterSuccessfulRemove,
            ErrorKind.FailedToCancelMatchmakingWithTimeout => ErrorKindDto.FailedToCancelMatchmakingWithTimeout,
            ErrorKind.RoomPrivate => ErrorKindDto.RoomPrivate,
            ErrorKind.OnPlayerReadyRejected => ErrorKindDto.OnPlayerReadyRejected,
            ErrorKind.OnStartMatchmakingRejected => ErrorKindDto.OnStartMatchmakingRejected,
            ErrorKind.MatchNotFound => ErrorKindDto.MatchNotFound,
            ErrorKind.CoinDoesNotExist => ErrorKindDto.CoinDoesNotExist,
            ErrorKind.ConfigurationNotValid => ErrorKindDto.ConfigurationNotValid,
            ErrorKind.AcceptedRoomIsOutdated => ErrorKindDto.AcceptedRoomIsOutdated,
            ErrorKind.RoomAlreadyInMatchedState => ErrorKindDto.RoomAlreadyInMatchedState,
            ErrorKind.JoinCodeMismatch => ErrorKindDto.JoinCodeMismatch,
            ErrorKind.Throttle => ErrorKindDto.Throttle,
            ErrorKind.InvalidMessage => ErrorKindDto.InvalidMessage,
            ErrorKind.ValidationError => ErrorKindDto.ValidationError,
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown enum value"),
        };

        public static ErrorBlame Map(this ErrorBlameDto dto) => dto switch
        {
            ErrorBlameDto.InternalServerError => ErrorBlame.InternalServerError,
            ErrorBlameDto.GameDeveloperError => ErrorBlame.GameDeveloperError,
            ErrorBlameDto.UserError => ErrorBlame.UserError,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static ErrorBlameDto Map(this ErrorBlame entity) => entity switch
        {
            ErrorBlame.InternalServerError => ErrorBlameDto.InternalServerError,
            ErrorBlame.GameDeveloperError => ErrorBlameDto.GameDeveloperError,
            ErrorBlame.UserError => ErrorBlameDto.UserError,
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown enum value"),
        };

        public static LeavingReason Map(this LeavingReasonDto dto) => dto switch
        {
            LeavingReasonDto.RoomClosed => LeavingReason.RoomClosed,
            LeavingReasonDto.UserLeft => LeavingReason.UserLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static MatchmakingState Map(this MatchmakingStateDto dto) => dto switch
        {
            MatchmakingStateDto.Unlocked => MatchmakingState.Unlocked,
            MatchmakingStateDto.RequestingMatchmaking => MatchmakingState.RequestingMatchmaking,
            MatchmakingStateDto.Matchmaking => MatchmakingState.Matchmaking,
            MatchmakingStateDto.CancellingMatchmaking => MatchmakingState.CancellingMatchmaking,
            MatchmakingStateDto.Matched => MatchmakingState.Matched,
            MatchmakingStateDto.Playing => MatchmakingState.Playing,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static MatchmakingStateDto Map(this MatchmakingState entity) => entity switch
        {
            MatchmakingState.Unlocked => MatchmakingStateDto.Unlocked,
            MatchmakingState.RequestingMatchmaking => MatchmakingStateDto.RequestingMatchmaking,
            MatchmakingState.Matchmaking => MatchmakingStateDto.Matchmaking,
            MatchmakingState.CancellingMatchmaking => MatchmakingStateDto.CancellingMatchmaking,
            MatchmakingState.Matched => MatchmakingStateDto.Matched,
            MatchmakingState.Playing => MatchmakingStateDto.Playing,
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown enum value"),
        };

        public static MatchData Map(this MatchDataDto dto) => new(
            dto.MatchId,
            dto.State.Map(),
            dto.MatchDetails?.Map(),
            dto.FailReason);

        public static MatchDataDto Map(this MatchData entity) => new(
            entity.MatchId,
            entity.State.Map(),
            entity.MatchDetails?.Map(),
            entity.FailReason);

        public static MatchDetails Map(this MatchDetailsDto dto) => new(
            dto.MatchedPlayersId,
            dto.TcpUdpServerAddress,
            dto.WebServerAddress,
            dto.UserSecret,
            dto.GameEngineData,
            dto.MatchmakerData);

        public static MatchDetailsDto Map(this MatchDetails entity) => new(
            entity.MatchedPlayersId,
            entity.TcpUdpServerAddress,
            entity.WebServerAddress,
            entity.UserSecret,
            entity.GameEngineData,
            entity.MatchmakerData);

        public static MatchState Map(this MatchStateDto dto) => dto switch
        {
            MatchStateDto.Initializing => MatchState.Initializing,
            MatchStateDto.Running => MatchState.Running,
            MatchStateDto.RunningEnded => MatchState.RunningEnded,
            MatchStateDto.InitializingFailed => MatchState.InitializingFailed,
            MatchStateDto.RunningFailed => MatchState.RunningFailed,
            MatchStateDto.ProcessCrashed => MatchState.ProcessCrashed,
            MatchStateDto.MatchmakingFailed => MatchState.MatchmakingFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown enum value"),
        };

        public static MatchStateDto Map(this MatchState entity) => entity switch
        {
            MatchState.Initializing => MatchStateDto.Initializing,
            MatchState.Running => MatchStateDto.Running,
            MatchState.RunningEnded => MatchStateDto.RunningEnded,
            MatchState.InitializingFailed => MatchStateDto.InitializingFailed,
            MatchState.RunningFailed => MatchStateDto.RunningFailed,
            MatchState.ProcessCrashed => MatchStateDto.ProcessCrashed,
            MatchState.MatchmakingFailed => MatchStateDto.MatchmakingFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown enum value"),
        };

        public static RoomBetDetails Map(this RoomBetDetailsDto dto) => new(
            dto.BetValueRaw,
            dto.Coin.Map(),
            dto.RollingBet?.Map());

        public static RoomCoin Map(this RoomCoinDto dto) => new()
        {
            CoinId = dto.CoinId,
            Chain = dto.Chain.Map(),
            Currency = dto.Currency.Map(),
        };

        public static RoomChain Map(this RoomChainDto dto) => new()
        {
            ExternalId = dto.ExternalId,
            Type = dto.Type.Map(),
            Name = dto.Name,
        };

        public static RoomCurrency Map(this RoomCurrencyDto dto) => new()
        {
            Ticker = dto.Ticker,
            Address = dto.Address,
            Decimals = dto.Decimals,
            IconUrl = dto.IconUrl,
        };

        public static RollingBet Map(this RollingBetDto dto) => new(
            dto.RollingBetId,
            dto.NumberOfPlayers,
            dto.EntryFee,
            dto.Prize);

        public static UserInfo Map(this UserInfoDto dto) => new(
            dto.UserId,
            dto.TeamIndex,
            dto.IsReady,
            dto.Nickname,
            dto.AvatarUrl,
            dto.CustomPlayerData);

        public static UserInfoDto Map(this UserInfo entity) => new(
            entity.UserId,
            entity.TeamIndex,
            entity.IsReady,
            entity.Nickname,
            entity.AvatarUrl,
            entity.CustomPlayerData);
    }
}
