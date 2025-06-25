using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record CreateRoom(
        [property: Key(1)] string RoomName,
        [property: Key(2)] bool IsPrivate,
        [property: Key(3)] bool IsEphemeral,
        [property: Key(4)] string QueueName,
        [property: Key(5)] bool IsSingleTeam,
        [property: Key(6)] IReadOnlyDictionary<string, string> CustomRoomData,
        [property: Key(7)] IReadOnlyDictionary<string, string> CustomMatchmakingData,
        [property: Key(8)] RoomTournamentDetails? TournamentDetails, //TO DO: Start using this in SDK and on backend instead of CustomMatchmakingData
        [property: Key(9)] RoomBetDetailsSlim? BetDetailsSlim,
        [property: Key(10)] Guid? RollingTournamentBetConfigId) : LobbyOperation
    {
        [SerializationConstructor]
        public CreateRoom(
            Guid operationId,
            string roomName,
            bool isPrivate,
            bool isEphemeral,
            string queueName,
            bool isSingleTeam,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            RoomTournamentDetails? tournamentDetails,
            RoomBetDetailsSlim? betDetailsSlim,
            Guid? rollingTournamentBetConfigId) : this(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData, tournamentDetails, betDetailsSlim, rollingTournamentBetConfigId) =>
            OperationId = operationId;

        public virtual bool Equals(CreateRoom? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomName == other.RoomName
                && QueueName == other.QueueName
                && IsSingleTeam == other.IsSingleTeam
                && IsPrivate == other.IsPrivate
                && IsEphemeral == other.IsEphemeral
                && CustomRoomData.Count == other.CustomRoomData.Count
                && CustomRoomData.IsTheSame(other.CustomRoomData)
                && CustomMatchmakingData.Count == other.CustomMatchmakingData.Count
                && CustomMatchmakingData.IsTheSame(other.CustomMatchmakingData);
        }

        public override int GetHashCode() => HashCode.Combine(RoomName, QueueName, IsSingleTeam, IsPrivate, IsEphemeral, CustomRoomData.Count, CustomMatchmakingData.Count);
    }
}
