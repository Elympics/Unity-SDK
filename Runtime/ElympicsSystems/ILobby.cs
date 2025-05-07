#nullable enable
using Elympics.SnapshotAnalysis.Retrievers;
namespace Elympics
{
    internal interface ILobby
    {
        SnapshotAnalysisRetriever? SnapshotAnalysisRetriever { get; }
    }
}
