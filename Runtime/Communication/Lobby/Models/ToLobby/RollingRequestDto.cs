using System;
using MessagePack;

#nullable enable

namespace Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public record RollingRequestDto(
        [property: Key(0)] Guid CoinId,
        [property: Key(1)] string Prize,
        [property: Key(2)] uint PlayersCount,
        [property: Key(3)] decimal[] PrizeDistribution);
}
