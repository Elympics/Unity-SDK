using System.Threading;
using Cysharp.Threading.Tasks;
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

        UniTask StartMatchmaking(IRoom room);

        UniTask CancelMatchmaking(IRoom room, CancellationToken ct);
        void MatchFound();
    }
}
