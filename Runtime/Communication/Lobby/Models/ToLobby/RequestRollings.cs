using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

namespace Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public record RequestRollings(
        [property: Key(1)] Guid GameId,
        [property: Key(2)] string VersionId,
        [property: Key(3)] List<RollingRequestDto> Rollings
        ) : LobbyOperation
    {
        [SerializationConstructor]
        public RequestRollings(Guid operationId, Guid GameId, string VersionId, List<RollingRequestDto> Rollings) : this(GameId, VersionId, Rollings) => OperationId = operationId;
    }

    [MessagePackObject]
    public record RollingRequestDto(
        [property: Key(0)] Guid CoinId,
        [property: Key(1)] string Prize,
        [property: Key(2)] uint PlayersCount,
        [property: Key(3)] float[] PrizeDistribution);

}
