using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elympics.Tests.Rooms
{
    public class RoomEventObserver
    {
        private readonly List<string> _expectedEvents = new();
        private readonly Dictionary<string, int> _calledHandlers = new();

        public RoomEventObserver(IRoomsManager roomsManager)
        {
            var events = roomsManager.GetType().GetEvents();
            foreach (var roomEvent in events)
            {
                _calledHandlers.Add(roomEvent.Name, 0);
                var factoryMethod = GetType()
                    .GetMethod(nameof(CreateEventHandler), BindingFlags.Instance | BindingFlags.NonPublic);
                var specializedFactory = factoryMethod!.MakeGenericMethod(roomEvent.EventHandlerType.GetGenericArguments().First());
                var handler = specializedFactory.Invoke(this, new object[] { roomEvent.Name });
                roomEvent.AddEventHandler(roomsManager, (Delegate)handler);
            }
        }

        private Action<TArgs> CreateEventHandler<TArgs>(string eventName) =>
            args => _ = _calledHandlers[eventName] += 1;

        public const string RoomListUpdatedInvoked = nameof(IRoomsManager.RoomListUpdated);
        public const string JoinedRoomUpdatedInvoked = nameof(IRoomsManager.JoinedRoomUpdated);
        public const string JoinedRoomInvoked = nameof(IRoomsManager.JoinedRoom);
        public const string LeftRoomInvoked = nameof(IRoomsManager.LeftRoom);
        public const string UserJoinedInvoked = nameof(IRoomsManager.UserJoined);
        public const string UserLeftInvoked = nameof(IRoomsManager.UserLeft);
        public const string UserCountChangedInvoked = nameof(IRoomsManager.UserCountChanged);
        public const string HostChangedInvoked = nameof(IRoomsManager.HostChanged);
        public const string UserReadinessChangedInvoked = nameof(IRoomsManager.UserReadinessChanged);
        public const string UserChangedTeamInvoked = nameof(IRoomsManager.UserChangedTeam);
        public const string CustomRoomDataChangedInvoked = nameof(IRoomsManager.CustomRoomDataChanged);
        public const string RoomNameChangedInvoked = nameof(IRoomsManager.RoomNameChanged);
        public const string RoomPublicnessChangedInvoked = nameof(IRoomsManager.RoomPublicnessChanged);
        public const string MatchmakingStartedInvoked = nameof(IRoomsManager.MatchmakingStarted);
        public const string MatchmakingEndedInvoked = nameof(IRoomsManager.MatchmakingEnded);
        public const string MatchmakingDataChangedInvoked = nameof(IRoomsManager.MatchmakingDataChanged);
        public const string MatchDataReceivedInvoked = nameof(IRoomsManager.MatchDataReceived);
        public const string CustomMatchmakingDataChanged = nameof(IRoomsManager.CustomMatchmakingDataChanged);

        public void ResetInvocationStatusAndRegisterAssertion(params string[] invokedToTrue)
        {
            Reset();
            _expectedEvents.AddRange(invokedToTrue);
        }
        public void AssertIfInvoked()
        {
            foreach (var handler in _calledHandlers)
            {
                var howManyTimesShouldBeInvoked = _expectedEvents.Count(x => x == handler.Key);
                TestContext.Out.WriteLine($"Checking {handler.Key} should be invoked {howManyTimesShouldBeInvoked} times.");
                Assert.AreEqual(howManyTimesShouldBeInvoked, handler.Value, $"Invoked {handler.Key} {handler.Value} times instead of {howManyTimesShouldBeInvoked}.");
            }
        }

        public void Reset()
        {
            var keyCollection = _calledHandlers.Keys.ToArray();
            foreach (var key in keyCollection)
                _calledHandlers[key] = 0;

            _expectedEvents.Clear();
        }
    }
}
