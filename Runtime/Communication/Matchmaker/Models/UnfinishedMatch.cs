using System;

namespace Elympics.Models.Matchmaking
{
    [Serializable]
    public class UnfinishedMatch
    {
        public string MatchId;
        public string QueueName;
        public string RegionName;
        public string InitializedAt;
    }
}
