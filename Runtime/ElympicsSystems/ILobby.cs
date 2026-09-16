#nullable enable
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal interface ILobby
    {
        AuthData? AuthData { get; }
        SnapshotAnalysisRetriever? SnapshotAnalysisRetriever { get; }
        MatchmakingFinishedData? MatchDataGuid { get; }
        JoinedMatchMode MatchMode { get; }
        void PlayMatchInternal(MatchmakingFinishedData matchData);
    }
}
