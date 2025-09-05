using System;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomBetDetailsSlim([property: Key(0)] string BetValue, [property: Key(1)] Guid CoinId, [property: Key(2)] Guid? RollingBetId);
}
