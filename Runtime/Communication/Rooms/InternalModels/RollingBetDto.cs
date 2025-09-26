using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record RollingBetDto([property: Key(0)] Guid RollingBetId, [property: Key(1)] int? NumberOfPlayers, [property: Key(2)] string EntryFee, [property: Key(3)] string Prize)
    {
        public virtual bool Equals(RollingBetDto? other) => RollingBetId == other?.RollingBetId;
        public override int GetHashCode() => RollingBetId.GetHashCode();
    }
}
