#nullable enable
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal interface ILobby
    {
        public AuthData? AuthData { get; }
        SnapshotAnalysisRetriever? SnapshotAnalysisRetriever { get; }
        MatchmakingFinishedData? MatchDataGuid { get; }
        JoinedMatchMode MatchMode { get; }
        public void PlayMatchInternal(MatchmakingFinishedData matchData);
    }
}
