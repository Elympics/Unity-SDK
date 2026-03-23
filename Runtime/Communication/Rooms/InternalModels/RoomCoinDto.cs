using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record RoomCoinDto(
        [property: Key(0)] Guid CoinId,
        [property: Key(1)] RoomChainDto Chain,
        [property: Key(2)] RoomCurrencyDto Currency);
}
