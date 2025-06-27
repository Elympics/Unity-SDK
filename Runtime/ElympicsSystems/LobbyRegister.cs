using System;
using Elympics.SnapshotAnalysis.Retrievers;
namespace Elympics
{
    internal static class LobbyRegister
    {
        public static ILobby ElympicsLobby;
        public static ILobby PlayPadLobby;

        public static SnapshotAnalysisRetriever GetSnapshotRetriever()
        {
            if (PlayPadLobby is { SnapshotAnalysisRetriever: not null })
                return PlayPadLobby.SnapshotAnalysisRetriever;
            if (ElympicsLobby is { SnapshotAnalysisRetriever: not null })
                return ElympicsLobby.SnapshotAnalysisRetriever;
            throw new InvalidOperationException("Couldn't find snapshot retriever.");
        }
    }
}
