using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elympics.AssemblyCommunicator;
using NUnit.Framework;

namespace Elympics.Tests.AssemblyCommunicator
{
    public class ElympicsEventTests
    {
        private static readonly int[] Counts = new int[] { 1, 2, 3, 5, 32 };
        private static IEnumerable<TestCaseData> RaiseMultipleCases()
        {
            foreach (var i in Counts)
            {
                foreach (var j in Counts)
                {
                    yield return new TestCaseData(i, j);
                }
            }
        }

        private class TestEvent { }

        private class TestObserver : IElympicsObserver<TestEvent>
        {
            public static int TotalCallCount = 0;

            private readonly TestEvent _expectedArgument;
            private int _callCount = 0;

            public TestObserver(TestEvent expectedArgument) => _expectedArgument = expectedArgument;

            public void OnEvent(TestEvent argument)
            {
                TotalCallCount++;
                _callCount++;
                Assert.AreSame(_expectedArgument, argument);
            }

            public void AssertCallCount(int expected) => Assert.AreEqual(expected, _callCount);
        }

        [Test]
        public void Raise()
        {
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var argument = new TestEvent();
            var observer = new TestObserver(argument);
            elympicsEvent.AddObserver(observer);
            elympicsEvent.Raise(argument);
            observer.AssertCallCount(1);
        }

        [TestCaseSource(nameof(RaiseMultipleCases))]
        public void RaiseMultiple(int observerCount, int callCount)
        {
            TestObserver.TotalCallCount = 0;
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var argument = new TestEvent();
            var observers = new TestObserver[observerCount];

            for (var i = 0; i < observerCount; i++)
            {
                var observer = new TestObserver(argument);
                elympicsEvent.AddObserver(observer);
                observers[i] = observer;
            }

            for (var i = 0; i < callCount; i++)
                elympicsEvent.Raise(argument);

            foreach (var observer in observers)
                observer.AssertCallCount(callCount);

            Assert.AreEqual(observerCount * callCount, TestObserver.TotalCallCount);
        }

        [Test]
        public void RaiseWithNoObservers()
        {
            TestObserver.TotalCallCount = 0;
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var argument = new TestEvent();
            elympicsEvent.Raise(argument);
            Assert.AreEqual(0, TestObserver.TotalCallCount);
        }

        [Test]
        public void RaiseWithNullArgument()
        {
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var observer = new TestObserver(null);
            elympicsEvent.AddObserver(observer);
            elympicsEvent.Raise(null);
            observer.AssertCallCount(1);
        }

        [Test]
        public void RaiseAfterGC()
        {
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var argument = new TestEvent();
            AddObserverInNewScope(elympicsEvent, argument); //Add observer, but don't save any references to it, so it will be eligible for garbage collection
            TestObserver.TotalCallCount = 0;
            GC.Collect();
            elympicsEvent.Raise(argument);
            Assert.AreEqual(0, TestObserver.TotalCallCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddObserverInNewScope(ElympicsEvent<TestEvent> elympicsEvent, TestEvent argument) => elympicsEvent.AddObserver(new TestObserver(argument));

        [Test]
        public void AddSameManyTimes()
        {
            var elympicsEvent = new ElympicsEvent<TestEvent>();
            var observer = new TestObserver(null);
            elympicsEvent.AddObserver(observer);
            _ = Assert.Throws<InvalidOperationException>(() => elympicsEvent.AddObserver(observer));
            _ = Assert.Throws<InvalidOperationException>(() => elympicsEvent.AddObserver(observer));
        }
    }
}
