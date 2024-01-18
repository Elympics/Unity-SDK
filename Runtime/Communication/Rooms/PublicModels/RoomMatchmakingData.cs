using System.Collections.Generic;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    public record RoomMatchmakingData(
        MatchmakingState MatchmakingState,
        string QueueName,
        uint TeamSize,
        uint TeamCount,
        MatchData? MatchData,
        IReadOnlyDictionary<string, string> CustomData)
    {

        public bool Equals(MatchmakingData? matchmakingData)
        {
            if (matchmakingData is null)
                return false;
            return MatchmakingState == matchmakingData.State
                && QueueName == matchmakingData.QueueName
                && TeamSize == matchmakingData.TeamSize
                && TeamCount == matchmakingData.TeamCount
                && Equals(MatchData, matchmakingData.MatchData)
                && CustomData.Count == matchmakingData.CustomData.Count
                && CustomData.IsTheSame(matchmakingData.CustomData);
        }

        public RoomMatchmakingData(MatchmakingData data)
            : this(data.State, data.QueueName, data.TeamSize, data.TeamCount, data.MatchData, data.CustomData)
        { }

        public RoomMatchmakingData(PublicMatchmakingData data)
            : this(data.State, data.QueueName, data.TeamSize, data.TeamCount, null, data.CustomData)
        { }
    }
}
