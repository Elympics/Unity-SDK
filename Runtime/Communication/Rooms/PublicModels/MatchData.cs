using System;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record MatchData(
        [property: Key(0)] Guid MatchId,
        [property: Key(1)] MatchState State,
        [property: Key(2)] MatchDetails? MatchDetails,
        [property: Key(3)] string? FailReason)
    {
        public virtual bool Equals(MatchData? other)
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
