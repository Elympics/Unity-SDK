using Elympics.Models.Matchmaking;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IMatchLauncher
    {
        bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        bool IsCurrentlyInMatch { get; }
        MatchmakingFinishedData? MatchDataGuid { get; }

        void PlayMatch(MatchmakingFinishedData matchData);
    }
}
