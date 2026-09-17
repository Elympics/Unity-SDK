#nullable enable

using Elympics.Models.Matchmaking;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public interface IMatchLauncher
    {
        bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        bool IsCurrentlyInMatch { get; }
        MatchmakingFinishedData? MatchDataGuid { get; }

        void PlayMatch(MatchmakingFinishedData matchData);

        void MatchmakingCompleted();
    }
}
