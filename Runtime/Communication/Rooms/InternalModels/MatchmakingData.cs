using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record MatchmakingData(
        [property: Key(0)] DateTime LastStateUpdate,
        [property: Key(1)] MatchmakingStateDto State,
        [property: Key(2)] string QueueName,
        [property: Key(3)] uint TeamCount,
        [property: Key(4)] uint TeamSize,
        [property: Key(5)] IReadOnlyDictionary<string, string> CustomData,
        [property: Key(6)] MatchDataDto? MatchData,
        [property: Key(7)] RoomTournamentDetails? TournamentDetails,
        [property: Key(8)] RoomBetDetailsDto? BetDetails)
    {
        public virtual bool Equals(MatchmakingData? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return State == other.State
                && LastStateUpdate.Equals(other.LastStateUpdate)
                && QueueName == other.QueueName
                && TeamSize == other.TeamSize
                && TeamCount == other.TeamCount
                && Equals(MatchData, other.MatchData)
                && CustomData.Count == other.CustomData.Count
                && !CustomData.Except(other.CustomData).Any();
        }

        public override string ToString() => $"{nameof(LastStateUpdate)}:{LastStateUpdate::HH:mm:ss.ffff}{Environment.NewLine}"
            + $"{nameof(State)}:{State}{Environment.NewLine}"
            + $"{nameof(QueueName)}:{QueueName}{Environment.NewLine}"
            + $"{nameof(TeamCount)}:{TeamCount}{Environment.NewLine}"
            + $"{nameof(TeamSize)}:{TeamSize}{Environment.NewLine}"
            + $"{nameof(CustomData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", CustomData?.Select(kv => $"Key = {kv.Key}, Value = {kv.Value}"))}{Environment.NewLine}"
            + $"{nameof(MatchData)}:{Environment.NewLine}\t{MatchData?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}"
            + $"{nameof(BetDetails)}:{Environment.NewLine}\t{BetDetails?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}";

        public override int GetHashCode() => HashCode.Combine(State, LastStateUpdate, QueueName, TeamSize, TeamCount, MatchData, CustomData.Count);
    }
}
