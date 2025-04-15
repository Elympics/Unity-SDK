using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;
using UnityEngine;

#nullable enable

namespace Elympics.Tests.Common.RoomMocks
{
    public class MatchLauncherMock : IMatchLauncher
    {
        public bool ShouldLoadGameplaySceneAfterMatchmaking { get; set; }

        public bool IsCurrentlyInMatch { get; set; }
        public MatchmakingFinishedData? MatchDataGuid { get; set; }

        public void PlayMatch(MatchmakingFinishedData matchData) => PlayMatchCalledArgs = matchData;
        public UniTask StartMatchmaking(IRoom room)
        {
            Debug.Log($"[{nameof(MatchLauncherMock)}] Start matchmaking");
            return room.StartMatchmakingInternal();
        }
        public UniTask CancelMatchmaking(IRoom room, CancellationToken ct)
        {
            Debug.Log($"[{nameof(MatchLauncherMock)}] Matchmaking canceled");
            return room.CancelMatchmakingInternal(ct);
        }
        public void MatchmakingCompleted() => Debug.Log($"[{nameof(MatchLauncherMock)}] Match found");

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
