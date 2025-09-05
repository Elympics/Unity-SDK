using System.Collections.Generic;
using Elympics.Communication.Rooms.InternalModels;
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
        IReadOnlyDictionary<string, string> CustomData,
        RoomBetDetails? BetDetails)
    {

        public bool Equals(MatchmakingData? matchmakingData)
        {
            if (matchmakingData is null)
                return false;
            return MatchmakingState == matchmakingData.State.Map()
                && QueueName == matchmakingData.QueueName
                && TeamSize == matchmakingData.TeamSize
                && TeamCount == matchmakingData.TeamCount
                && Equals(MatchData, matchmakingData.MatchData?.Map())
                && CustomData.Count == matchmakingData.CustomData.Count
                && CustomData.IsTheSame(matchmakingData.CustomData)
                && BetDetails == matchmakingData.BetDetails?.Map();
        }

        internal RoomMatchmakingData(MatchmakingData data)
            : this(data.State.Map(), data.QueueName, data.TeamSize, data.TeamCount, data.MatchData?.Map(), data.CustomData, data.BetDetails?.Map())
        { }

        public RoomMatchmakingData(PublicMatchmakingData data)
            : this(data.State.Map(), data.QueueName, data.TeamSize, data.TeamCount, null, data.CustomData, data.BetDetails?.Map())
        { }
    }
}
