using System;
using System.Collections.Generic;
using Elympics.Communication.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record PublicMatchmakingData(
        [property: Key(0)] DateTime LastStateUpdate,
        [property: Key(1)] MatchmakingStateDto State,
        [property: Key(2)] string QueueName,
        [property: Key(3)] uint TeamCount,
        [property: Key(4)] uint TeamSize,
        [property: Key(5)] IReadOnlyDictionary<string, string> CustomData,
        [property: Key(6)] RoomBetDetailsDto? BetDetails);
}
