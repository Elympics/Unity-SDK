using System;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;
using JetBrains.Annotations;
namespace Elympics
{
    internal static class LobbyRegister
    {
        public static ILobby ElympicsLobby;
        public static ILobby PlayPadLobby;


        public static bool IsLobbyRegistered() => PlayPadLobby != null || ElympicsLobby != null;

        public static SnapshotAnalysisRetriever GetSnapshotRetriever()
        {
            if (PlayPadLobby is { SnapshotAnalysisRetriever: not null })
                return PlayPadLobby.SnapshotAnalysisRetriever;
            if (ElympicsLobby is { SnapshotAnalysisRetriever: not null })
                return ElympicsLobby.SnapshotAnalysisRetriever;
            throw new InvalidOperationException("Couldn't find snapshot retriever.");
        }

        public static JoinedMatchMode GetJoinedMatchMode()
        {
            if (PlayPadLobby != null)
                return PlayPadLobby.MatchMode;
            if (ElympicsLobby != null)
                return ElympicsLobby.MatchMode;
            throw new InvalidOperationException("No lobby registered.");
        }

        public static void PlayMatchInternal(MatchmakingFinishedData matchData) => PlayPadLobby?.PlayMatchInternal(matchData);

        public static bool IsAuthenticated() => PlayPadLobby.AuthData != null || ElympicsLobby.AuthData != null;

        [CanBeNull]
        public static MatchmakingFinishedData GetMatchData()
        {
            if (PlayPadLobby?.MatchDataGuid != null)
                return PlayPadLobby.MatchDataGuid;
            if (ElympicsLobby?.MatchDataGuid != null)
                return ElympicsLobby.MatchDataGuid;
            return null;
        }

        public static AuthData GetAuthData()
        {
            if (PlayPadLobby?.AuthData != null)
                return PlayPadLobby.AuthData;
            if (ElympicsLobby?.AuthData != null)
                return ElympicsLobby.AuthData;
            return null;
        }
    }
}
