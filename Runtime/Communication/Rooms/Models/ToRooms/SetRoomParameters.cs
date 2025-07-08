using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record SetRoomParameters(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] string? RoomName,
        [property: Key(3)] bool? IsPrivate,
        [property: Key(4)] IReadOnlyDictionary<string, string>? CustomRoomData,
        [property: Key(5)] IReadOnlyDictionary<string, string>? CustomMatchmakingData,
        [property: Key(6)] RoomTournamentDetails? TournamentDetails, //TO DO: Start using this in SDK and on backend instead of CustomMatchmakingData
        [property: Key(7)] RoomBetDetailsSlim? BetDetailsSlim) : LobbyOperation
    {
        [SerializationConstructor]
        public SetRoomParameters(
            Guid operationId,
            Guid roomId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData,
            RoomTournamentDetails? tournamentDetails,
            RoomBetDetailsSlim? betDetailsSlim) : this(roomId, roomName, isPrivate, customRoomData, customMatchmakingData, tournamentDetails, betDetailsSlim) =>
            OperationId = operationId;

        public virtual bool Equals(SetRoomParameters? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && RoomName == other.RoomName
                && IsPrivate == other.IsPrivate
                && CustomRoomData.IsTheSame(other.CustomRoomData)
                && CustomMatchmakingData.IsTheSame(other.CustomMatchmakingData);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), RoomId, RoomName ?? "", IsPrivate, CustomRoomData?.Count ?? 0, CustomMatchmakingData?.Count ?? 0);
    }

    [MessagePackObject]
    public record RoomBetDetailsSlim([property: Key(0)] string BetValue, [property: Key(1)] Guid CoinId, [property: Key(2)] int? NumberOfPlayers);
}
