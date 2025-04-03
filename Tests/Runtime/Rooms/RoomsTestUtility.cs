using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Rooms.Models;

namespace Elympics.Tests.Rooms
{
    internal static class RoomsTestUtility
    {
        public static RoomStateChanged PrepareInitialRoomState(Guid roomId, int userCount = 1, MatchmakingState mmState = MatchmakingState.Unlocked)
        {
            var userList = new List<UserInfo>();
            for (var i = 0; i < userCount; i++)
                userList.Add(new UserInfo(Guid.NewGuid(), 0, false, string.Empty, null));

            var roomState = new RoomStateChanged(roomId,
                DateTime.Now,
                "testName",
                "test join code",
                true,
                new MatchmakingData(DateTime.Now,
                    mmState,
                    "testQueue",
                    1,
                    1,
                    new Dictionary<string, string>(),
                    mmState == MatchmakingState.Playing ? GetDummyMatchData(userList.Select(x => x.UserId).ToList()) : null,
                    null,
                    null),
                userList,
                false,
                false,
                new Dictionary<string, string>());
            return roomState;
        }

        public static PublicRoomState PrepareNotJoinedRoomState(Guid roomId, int userCount = 1, MatchmakingState mmState = MatchmakingState.Unlocked)
        {
            var userList = new List<UserInfo>();
            for (var i = 0; i < userCount; i++)
                userList.Add(new UserInfo(Guid.NewGuid(), 0, false, string.Empty, null));

            return new PublicRoomState(roomId, DateTime.Now, "testName", true, new PublicMatchmakingData(DateTime.Now, mmState, "testQueue", 1, 1, new Dictionary<string, string>(), null), userList, false, new Dictionary<string, string>());
        }

        public static MatchmakingData GetMatchmakingDataForState(MatchmakingState state) => new(DateTime.Now, state, "testQueue", 1, 1, new Dictionary<string, string>(), GetDummyMatchData(null), null, null);

        public static MatchData GetDummyMatchData(List<Guid> matchedPlayers) =>
            new(Guid.NewGuid(), MatchState.Running, GetDummyMatchDetails(matchedPlayers), null);

        public static MatchDetails GetDummyMatchDetails(List<Guid> matchedPlayers) =>
            new(matchedPlayers, string.Empty, string.Empty, string.Empty, Array.Empty<byte>(), Array.Empty<float>());
    }
}
