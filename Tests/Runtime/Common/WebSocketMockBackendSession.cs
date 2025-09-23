using System;
using System.Collections.Generic;
using Elympics.Rooms.Models;

namespace Elympics
{
    public class WebSocketMockBackendSession
    {
        public Guid? PlayerCurrentRoom = null;
        public bool TracksRoomList = false;

        public readonly Dictionary<Guid, RoomStateChanged> Rooms = new();
        public readonly Dictionary<string, (uint TeamSize, uint TeamCount)> Queues = new();

        public WebSocketMockBackendSession()
        {
            var queueName1 = "q1";
            var queueName2 = "q2";
            var queueName3 = "q3";
            var queueName4 = "q4";

            Queues.Add(queueName1, (1, 2));
            Queues.Add(queueName2, (2, 2));
            Queues.Add(queueName3, (2, 5));
            Queues.Add(queueName4, (1, 6));

            var roomId1 = Guid.Parse("383dc2ee-e1bf-4224-aaeb-66e425da8702");
            var roomId2 = Guid.Parse("a5f30767-66f1-4cd0-80b5-924f6a0cafaa");
            var roomId3 = Guid.Parse("02c0fe92-ce91-475e-90bf-12c8cea23016");

            Rooms.Add(roomId1, new RoomStateChanged(
                roomId1,
                DateTime.Now,
                "Pair1v1Public",
                "AAAAAAAA",
                false,
                new MatchmakingData(
                    DateTime.Now,
                    MatchmakingState.Unlocked,
                    queueName1,
                    Queues[queueName1].TeamCount,
                    Queues[queueName1].TeamSize,
                    new Dictionary<string, string>(),
                    null,
                    null,
                    null),
                new List<UserInfo>
                {
                    new(Guid.NewGuid(), 0, false, "NewGuid_0", null, new Dictionary<string, string>())
                },
                false,
                false,
                new Dictionary<string, string>()

            ));

            Rooms.Add(roomId2, new RoomStateChanged(
                roomId2,
                DateTime.Now,
                "2v2Private",
                "BBBBBBBB",
                false,
                new MatchmakingData(
                    DateTime.Now,
                    MatchmakingState.Unlocked,
                    queueName2,
                    Queues[queueName2].TeamCount,
                    Queues[queueName2].TeamSize,
                    new Dictionary<string, string>(),
                    null,
                    null,
                    null),
                new List<UserInfo>
                {
                    new(Guid.NewGuid(), 0, false, "NewGuid_0", null, new Dictionary<string, string>()),
                    new(Guid.NewGuid(), 1, true, "NewGuid_1", null, new Dictionary<string, string>())
                },
                true,
                false,
                new Dictionary<string, string>()
            ));

            Rooms.Add(roomId3, new RoomStateChanged(
                roomId3,
                DateTime.Now,
                "1v1Private",
                "CCCCCCCC",
                false,
                new MatchmakingData(
                    DateTime.Now,
                    MatchmakingState.Unlocked,
                    queueName1,
                    Queues[queueName1].TeamCount,
                    Queues[queueName1].TeamSize,
                    new Dictionary<string, string>(),
                    null,
                    null,
                    null),
                new List<UserInfo>
                {
                    new(Guid.NewGuid(), 0, true, "NewGuid_0", null, new Dictionary<string, string>())
                },
                true,
                false,
                new Dictionary<string, string>()
            ));
        }
    }
}
