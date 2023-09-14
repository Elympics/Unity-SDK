using System;
using System.Collections.Generic;

namespace Elympics.Models.Matchmaking
{
    [Serializable]
    public class UnfinishedMatchesResponse
    {
        public List<UnfinishedMatch> Matches;
    }
}
