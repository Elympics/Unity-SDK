using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
            // Mono scans stacks conservatively, so a stale stack slot could keep the observer alive.
            // Creating it on a finished thread leaves no such slot.
            WeakReference observerReference = null;
            var thread = new Thread(() => observerReference = AddObserverInNewScope(elympicsEvent, argument));
            thread.Start();
            thread.Join();
            TestObserver.TotalCallCount = 0;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (observerReference.IsAlive)
                Assert.Ignore("The garbage collector didn't collect the observer, so there is nothing to test.");
            elympicsEvent.Raise(argument);
            Assert.AreEqual(0, TestObserver.TotalCallCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference AddObserverInNewScope(ElympicsEvent<TestEvent> elympicsEvent, TestEvent argument)
        {
            var observer = new TestObserver(argument);
            elympicsEvent.AddObserver(observer);
            return new WeakReference(observer);
        }

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
