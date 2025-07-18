using System;
using System.Collections.Generic;
using Elympics.Rooms.Models;
using NSubstitute;

namespace Elympics
{
    internal static class RoomsManagerMockSetup
    {
        public static IRoomsManager SetCurrentDefaultRoom(this IRoomsManager roomsManager, IRoomsClient roomClient, IMatchLauncher roomMatchClient)
        {
            IRoom room = new Room(roomMatchClient,
                roomClient,
                Guid.Empty,
                new RoomStateChanged(Guid.Empty,
                    DateTime.Now,
                    string.Empty,
                    null,
                    false,
                    new MatchmakingData(DateTime.Now,
                        MatchmakingState.Playing,
                        "test",
                        1,
                        1,
                        new Dictionary<string, string>(),
                        new MatchData(Guid.Empty, MatchState.Running, new MatchDetails(new List<Guid>(), null, null, null, null, null), null),
                        null,
                        null),
                    new List<UserInfo>() { new(Guid.Empty, 0, true, string.Empty, string.Empty) },
                    false,
                    false,
                    null));
            room.IsJoined = true;
            _ = roomsManager.ListJoinedRooms().Returns(new List<IRoom>()
            {
                room
            });

            _ = roomsManager.CurrentRoom.Returns((IRoom?)null);
            return roomsManager;
        }
    }
}
