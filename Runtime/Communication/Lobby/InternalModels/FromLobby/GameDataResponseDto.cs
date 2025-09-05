using System;
using System.Collections.Generic;
using Elympics.Communication.Rooms.InternalModels;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    [MessagePackObject]
    public record GameDataResponseDto(
        [property: Key(0)] int JoinedMatchRooms,
        [property: Key(1)] List<RoomCoinDto> CoinData,
        [property: Key(2)] string GameVersionId,
        [property: Key(3)] string FleetName,
        [property: Key(4)] Guid RequestId) : ILobbyResponse
    {
        public virtual bool Equals(GameDataResponseDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return JoinedMatchRooms == other.JoinedMatchRooms && GameVersionId == other.GameVersionId && FleetName == other.FleetName;
        }

        public override int GetHashCode() => JoinedMatchRooms.GetHashCode();

        public override string ToString() =>
            $"RoomsJoined: {JoinedMatchRooms} | {nameof(GameVersionId)}:{GameVersionId} | {nameof(FleetName)}:{FleetName} | Coins count: {(CoinData != null ? CoinData.Count : "No coins found.")}";
    }
}
