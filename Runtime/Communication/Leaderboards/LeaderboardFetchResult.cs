using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
    [Obsolete("Use PlayPadCommunicator.LeaderboardCommunicator from PlayPad SDK instead.")]
    public class LeaderboardFetchResult
    {
        public List<LeaderboardEntry> Entries { get; }
        public int TotalRecords { get; }
        public int PageNumber { get; }

        internal LeaderboardFetchResult(LeaderboardResponse response)
        {
            if (response == null)
                return;

            Entries = response.data?.Select(x => new LeaderboardEntry(x)).ToList();
            TotalRecords = response.totalRecords;
            PageNumber = response.pageNumber;
        }
    }
}
