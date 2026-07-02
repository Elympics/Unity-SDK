using System;
using Elympics.ElympicsSystems;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.ElympicsSystems
{
    [TestFixture]
    [Category("ElympicsSystems")]
    public class RxSnapshotTicksTrackerTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-1000)]
        public void NonPositiveWindowShouldResultInException(int milliseconds) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new RxSnapshotTicksTracker(TimeSpan.FromMilliseconds(milliseconds)));

        [Test]
        public void CosntructedStateShouldBeEmptyAndThrowNoExceptions()
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));

            var currentState = tracker.CurrentState;

            Assert.That(currentState.received, Is.EqualTo(0));
            Assert.That(currentState.total, Is.EqualTo(0));
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-1000)]
        public void InitializingWithNonPositiveTpsShouldResultInException(int ticksPerSecond)
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => tracker.Initialize(ticksPerSecond));
        }

        [Test]
        public void InitializedStateShouldBeEmptyAndThrowNoExceptions()
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));
            tracker.Initialize(30);

            var currentState = tracker.CurrentState;

            Assert.That(currentState.received, Is.EqualTo(0));
            Assert.That(currentState.total, Is.EqualTo(0));
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-1000)]
        public void UpdatingWithNonPositiveTpsShouldResultInException(long tick)
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));
            tracker.Initialize(30);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => tracker.Update(tick));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void UpdatingCorrectlyIncreasesReceivedAndTotalCount(int updateCount)
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));
            tracker.Initialize(30);

            for (var i = 1; i <= updateCount; i++)
                tracker.Update(i);

            Assert.That(tracker.CurrentState.received, Is.EqualTo(updateCount));
            Assert.That(tracker.CurrentState.total, Is.EqualTo(updateCount));
        }

        [Test]
        public void UpdatingCorrectlyRemovesTicksOutsideWindow()
        {
            const int ticksInWindow = 5;
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(1));
            tracker.Initialize(ticksInWindow);

            for (var i = 1; i <= ticksInWindow + 1; i++)
                tracker.Update(i);

            Assert.That(tracker.CurrentState.received, Is.EqualTo(ticksInWindow));
            Assert.That(tracker.CurrentState.total, Is.EqualTo(ticksInWindow));
        }

        [Test]
        public void SkippingTicksLeadsToReceivedDifferingFromTotal()
        {
            var tracker = new RxSnapshotTicksTracker(TimeSpan.FromSeconds(5));
            tracker.Initialize(30);

            tracker.Update(21);
            tracker.Update(37);

            Assert.That(tracker.CurrentState.received, Is.EqualTo(2));
            Assert.That(tracker.CurrentState.total, Is.EqualTo(17));
        }
    }
}
