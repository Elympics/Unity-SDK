using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class PlayerStateUpdateQueueTests
    {
        [Test]
        public void DrainTo_AppliesMaxWinsSemantics()
        {
            // Arrange
            var queue = new PlayerStateUpdateQueue();
            var playerLastReceived = new long[4];

            // Enqueue tick 100, then an older tick 95 for the same player
            queue.Enqueue(0, 100);
            queue.Enqueue(0, 95);

            var activePlayers = new[] { 0, 1, 2, 3 };
            var activePlayerCount = activePlayers.Length;
            // Act
            queue.DrainTo(playerLastReceived, new PackedArray<int>(activePlayers, activePlayerCount));

            // Assert - max-wins: 100 should be kept, 95 should be ignored
            Assert.That(playerLastReceived[0], Is.EqualTo(100));
        }

        [Test]
        public void DrainTo_MultiplePlayersIndependent()
        {
            // Arrange
            var queue = new PlayerStateUpdateQueue();
            var playerLastReceived = new long[4];

            queue.Enqueue(0, 50);
            queue.Enqueue(1, 75);
            queue.Enqueue(2, 30);
            queue.Enqueue(0, 60);

            var activePlayers = new[] { 0, 1, 2, 3 };
            var activePlayerCount = activePlayers.Length;
            // Act
            queue.DrainTo(playerLastReceived, new PackedArray<int>(activePlayers, activePlayerCount));

            // Assert - each player gets their own max value independently
            Assert.That(playerLastReceived[0], Is.EqualTo(60));
            Assert.That(playerLastReceived[1], Is.EqualTo(75));
            Assert.That(playerLastReceived[2], Is.EqualTo(30));
            Assert.That(playerLastReceived[3], Is.EqualTo(0)); // untouched
        }

        [Test]
        public void DrainTo_EmptyQueue_NoChanges()
        {
            // Arrange
            var queue = new PlayerStateUpdateQueue();
            var playerLastReceived = new long[] { 10, 20, 30, 40 };

            var activePlayers = new[] { 0, 1, 2, 3 };
            var activePlayerCount = activePlayers.Length;
            // Act
            queue.DrainTo(playerLastReceived, new PackedArray<int>(activePlayers, activePlayerCount));

            // Assert - values unchanged
            Assert.That(playerLastReceived[0], Is.EqualTo(10));
            Assert.That(playerLastReceived[1], Is.EqualTo(20));
            Assert.That(playerLastReceived[2], Is.EqualTo(30));
            Assert.That(playerLastReceived[3], Is.EqualTo(40));
        }

        [Test]
        public void Reset_ClearsPendingUpdates()
        {
            // Arrange
            var queue = new PlayerStateUpdateQueue();
            queue.Enqueue(0, 100);
            queue.Enqueue(1, 200);

            // Act - reset discards all pending updates
            queue.Reset();

            // Drain into fresh array
            var playerLastReceived = new long[4];
            var activePlayers = new[] { 0, 1, 2, 3 };
            var activePlayerCount = activePlayers.Length;
            queue.DrainTo(playerLastReceived, new PackedArray<int>(activePlayers, activePlayerCount));

            // Assert - nothing was applied
            Assert.That(playerLastReceived[0], Is.EqualTo(0));
            Assert.That(playerLastReceived[1], Is.EqualTo(0));
        }
    }
}
