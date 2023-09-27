using Elympics.Models.Matchmaking;

#nullable enable

namespace Elympics.Tests.Common.RoomMocks
{
    public class MatchLauncherMock : IMatchLauncher
    {
        public bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        public bool IsCurrentlyInMatch { get; set; }
        public MatchmakingFinishedData? MatchDataGuid { get; set; }

        public void PlayMatch(MatchmakingFinishedData matchData) => PlayMatchCalledArgs = matchData;

        public MatchmakingFinishedData? PlayMatchCalledArgs;

        public void Reset()
        {
            ShouldLoadGameplaySceneAfterMatchmaking = false;
            IsCurrentlyInMatch = false;
            MatchDataGuid = null;
            PlayMatchCalledArgs = null;
        }
    }
}
