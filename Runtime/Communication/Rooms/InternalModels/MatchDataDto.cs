using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record MatchDataDto(
        [property: Key(0)] Guid MatchId,
        [property: Key(1)] MatchStateDto State,
        [property: Key(2)] MatchDetailsDto? MatchDetails,
        [property: Key(3)] string? FailReason)
    {
        public virtual bool Equals(MatchDataDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return MatchId.Equals(other.MatchId)
                && State == other.State
                && Equals(MatchDetails, other.MatchDetails)
                && FailReason == other.FailReason;
        }

        public override string ToString() => $"{nameof(MatchId)}:{MatchId}{Environment.NewLine}"
            + $"{nameof(State)}:{State}{Environment.NewLine}"
            + $"{nameof(MatchDetails)}:{Environment.NewLine}\t{MatchDetails?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}"
            + $"{nameof(FailReason)}:{FailReason}{Environment.NewLine}";

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(MatchId);
            hashCode.Add(State);
            hashCode.Add(MatchDetails);
            hashCode.Add(FailReason);
            return hashCode.ToHashCode();
        }
    }
}
