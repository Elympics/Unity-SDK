using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elympics.Tests.Rooms
{
    public class EventObserver<T>
    {
        private readonly List<string> _expectedEvents = new();
        private readonly Dictionary<string, int> _calledHandlers = new();

        public EventObserver(T eventOwner)
        {
            var events = typeof(T)
                .GetEvents()
                .Where(e => e.EventHandlerType.GetGenericTypeDefinition() == typeof(Action<>));
            foreach (var eventInfo in events)
            {
                _calledHandlers.Add(eventInfo.Name, 0);
                var factoryMethod = GetType()
                    .GetMethod(nameof(CreateEventHandler), BindingFlags.Instance | BindingFlags.NonPublic);
                var specializedFactory = factoryMethod!.MakeGenericMethod(eventInfo.EventHandlerType.GetGenericArguments().First());
                var handler = specializedFactory.Invoke(this, new object[] { eventInfo.Name });
                eventInfo.AddEventHandler(eventOwner, (Delegate)handler);
            }
        }

        private Action<TArgs> CreateEventHandler<TArgs>(string eventName) =>
            args => _ = _calledHandlers[eventName] += 1;

        public void ListenForEvents(params string[] expectedEvents)
        {
            Reset();
            _expectedEvents.AddRange(expectedEvents);
        }

        public void AssertIfInvoked(bool shouldThrowOnUnexpectedEvents = true)
        {
            foreach (var handler in _calledHandlers)
            {
                var expectedInvocationCount = _expectedEvents.Count(x => x == handler.Key);
                var atLeast = shouldThrowOnUnexpectedEvents ? "" : "at least ";
                TestContext.Out.WriteLine($"Checking if {handler.Key} was invoked {atLeast}{expectedInvocationCount} times...");
                if (!shouldThrowOnUnexpectedEvents && handler.Value >= expectedInvocationCount)
                    continue;
                Assert.AreEqual(expectedInvocationCount, handler.Value, $"Invoked {handler.Key} {handler.Value} times instead of {expectedInvocationCount}.");
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
