using System.Collections.Generic;
using System.Linq;
using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class InterestManagementSystemTests
    {
        #region Helper Methods

        private static int[][] CreateJagged(int rows, int cols)
        {
            var arr = new int[rows][];
            for (var i = 0; i < rows; i++)
                arr[i] = new int[cols];
            return arr;
        }

        private static int[] CreateSparseToDense(params (int networkId, int denseIndex)[] mappings)
        {
            var array = new int[256];
            for (var i = 0; i < array.Length; i++)
                array[i] = -1;

            foreach (var (networkId, denseIndex) in mappings)
                array[networkId] = denseIndex;

            return array;
        }

        /// <summary>
        /// Creates a PackedArray&lt;int&gt; with active player indices [0..count-1].
        /// </summary>
        private static PackedArray<int> CreateActivePlayers(int count)
        {
            var array = new int[count];
            for (var i = 0; i < count; i++)
                array[i] = i;
            return new PackedArray<int>(array, count);
        }

        /// <summary>
        /// Creates a PackedArray&lt;int&gt; from an explicit list of player indices.
        /// </summary>
        private static PackedArray<int> CreateActivePlayers(int capacity, int[] playerIndices)
        {
            var array = new int[capacity];
            for (var i = 0; i < playerIndices.Length; i++)
                array[i] = playerIndices[i];
            return new PackedArray<int>(array, playerIndices.Length);
        }

        #endregion

        // =====================================================================
        // Category 1: ConvertVisibleFor
        // =====================================================================

        private static int[] maxPlayerAmount = Enumerable.Range(0, 33).ToArray();
        private static int[] playerIndexRange = Enumerable.Range(0, 32).ToArray();


        [Test]
        public void ConvertVisibleFor_All_ReturnsAllBitsSet([ValueSource(nameof(maxPlayerAmount))] int maxPlayers)
        {
            // Act
            var mask = InterestManagementSystem.ConvertVisibleFor(ElympicsPlayer.All, maxPlayers);

            // Assert
            Assert.That(mask, Is.EqualTo(maxPlayers == 32 ? uint.MaxValue : (1u << maxPlayers) - 1));
        }

        [Test]
        public void ConvertVisibleFor_World_ReturnsZero([ValueSource(nameof(maxPlayerAmount))] int maxPlayers)
        {
            // Act
            var mask = InterestManagementSystem.ConvertVisibleFor(ElympicsPlayer.World, maxPlayers);

            // Assert
            Assert.That(mask, Is.EqualTo(0u));
        }

        [Test]
        public void ConvertVisibleFor_PlayerIndex2_ReturnsBit2Set([ValueSource(nameof(playerIndexRange))] int currentPlayer)
        {
            // Arrange
            const int maxPlayers = 32;

            // Act
            var mask = InterestManagementSystem.ConvertVisibleFor(ElympicsPlayer.FromIndex(currentPlayer), maxPlayers);

            // Assert
            Assert.That(mask, Is.EqualTo(1u << currentPlayer));
        }

        [Test]
        public void ConvertVisibleFor_Invalid_ReturnsZero([ValueSource(nameof(maxPlayerAmount))] int maxPlayers)
        {
            // Act
            var mask = InterestManagementSystem.ConvertVisibleFor(ElympicsPlayer.Invalid, maxPlayers);

            // Assert
            Assert.That(mask, Is.EqualTo(0u));
        }

        // =====================================================================
        // Category 2: Execute
        // =====================================================================

        [Test]
        public void Execute_TwoEntities_DifferentMasks_CorrectPerPlayerLists()
        {
            // Arrange
            const int maxPlayers = 2;
            var currentData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1 },
                [20] = new byte[] { 2 }
            };
            var interestMask = new uint[2];
            interestMask[0] = 0b11; // Both players
            interestMask[1] = 0b01; // Player 0 only

            var sparseToDense = CreateSparseToDense((10, 0), (20, 1));
            var activePlayers = CreateActivePlayers(maxPlayers);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(maxPlayers, 128), new int[maxPlayers]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(2)); // Player 0 sees both
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(1)); // Player 1 sees only first

            Assert.That(relevantEntities[0, 0], Is.EqualTo(0)); // Dense index 0
            Assert.That(relevantEntities[0, 1], Is.EqualTo(1)); // Dense index 1
            Assert.That(relevantEntities[1, 0], Is.EqualTo(0)); // Dense index 0
        }

        [Test]
        public void Execute_NullCurrentData_NoOp()
        {
            // Arrange
            Dictionary<int, byte[]> currentData = null;
            var interestMask = new uint[2];
            var sparseToDense = CreateSparseToDense();
            var activePlayers = CreateActivePlayers(2);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);

            // Act & Assert - should not throw
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Counts should remain 0
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(0));
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(0));
        }

        [Test]
        public void Execute_WorldEntity_AppearsInNoLists()
        {
            // Arrange
            const int maxPlayers = 2;
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1 } };
            var interestMask = new uint[1];
            interestMask[0] = 0u; // World entity (mask = 0)

            var sparseToDense = CreateSparseToDense((10, 0));
            var activePlayers = CreateActivePlayers(maxPlayers);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(maxPlayers, 128), new int[maxPlayers]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert - no player sees it
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(0));
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(0));
        }

        [Test]
        public void Execute_UnregisteredEntity_Skipped()
        {
            // Arrange
            const int maxPlayers = 2;
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1 } };
            var interestMask = new uint[1];
            var sparseToDense = CreateSparseToDense(); // 10 not registered
            var activePlayers = CreateActivePlayers(maxPlayers);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(maxPlayers, 128), new int[maxPlayers]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert - no crash, nothing added
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(0));
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(0));
        }

        [Test]
        public void Execute_MultipleEntitiesSamePlayer_AccumulatesCorrectly()
        {
            // Arrange
            const int maxPlayers = 4;
            var currentData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1 },
                [20] = new byte[] { 2 },
                [30] = new byte[] { 3 }
            };
            var interestMask = new uint[3];
            interestMask[0] = 0b0010; // Player 1 only
            interestMask[1] = 0b0010; // Player 1 only
            interestMask[2] = 0b0010; // Player 1 only

            var sparseToDense = CreateSparseToDense((10, 0), (20, 1), (30, 2));
            var activePlayers = CreateActivePlayers(maxPlayers);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(maxPlayers, 128), new int[maxPlayers]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(0)); // Player 0 sees nothing
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(3)); // Player 1 sees all 3
            Assert.That(relevantEntities.RowCount(2), Is.EqualTo(0));
            Assert.That(relevantEntities.RowCount(3), Is.EqualTo(0));

            Assert.That(relevantEntities[1, 0], Is.EqualTo(0)); // Dense index 0
            Assert.That(relevantEntities[1, 1], Is.EqualTo(1)); // Dense index 1
            Assert.That(relevantEntities[1, 2], Is.EqualTo(2)); // Dense index 2
        }

        [Test]
        public void Execute_InactivePlayers_GetZeroRelevantEntities()
        {
            // Arrange - maxPlayers=4 but only players 0 and 2 active
            var currentData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1 }
            };
            var interestMask = new uint[1];
            interestMask[0] = 0b1111; // Entity visible to all players

            var sparseToDense = CreateSparseToDense((10, 0));
            var activePlayers = CreateActivePlayers(capacity: 4, playerIndices: new[] { 0, 2 });
            var relevantEntities = new PackedArray2D<int>(CreateJagged(4, 128), new int[4]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert - only active players get relevant entities
            Assert.That(relevantEntities.RowCount(0), Is.EqualTo(1)); // Active player 0
            Assert.That(relevantEntities.RowCount(1), Is.EqualTo(0)); // Inactive player 1
            Assert.That(relevantEntities.RowCount(2), Is.EqualTo(1)); // Active player 2
            Assert.That(relevantEntities.RowCount(3), Is.EqualTo(0)); // Inactive player 3

            Assert.That(relevantEntities[0, 0], Is.EqualTo(0)); // Dense index 0
            Assert.That(relevantEntities[2, 0], Is.EqualTo(0)); // Dense index 0
        }

        // =====================================================================
        // Category 3: 32-Player Boundary
        // =====================================================================

        [Test]
        public void ConvertVisibleFor_Player31_CorrectBit()
        {
            // Arrange
            const int maxPlayers = 32;
            var player31 = ElympicsPlayer.FromIndex(31);

            // Act
            var mask = InterestManagementSystem.ConvertVisibleFor(player31, maxPlayers);

            // Assert - bit 31 should be set (0x80000000)
            Assert.That(mask, Is.EqualTo(1u << 31));
            Assert.That(mask, Is.EqualTo(0x80000000u));
        }

        [Test]
        public void Execute_With32Players_AllVisible()
        {
            // Arrange
            const int maxPlayers = 32;
            var currentData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1 }
            };
            var interestMask = new uint[1];
            interestMask[0] = InterestManagementSystem.ConvertVisibleFor(ElympicsPlayer.All, maxPlayers);

            var sparseToDense = CreateSparseToDense((10, 0));
            var activePlayers = CreateActivePlayers(maxPlayers);
            var relevantEntities = new PackedArray2D<int>(CreateJagged(maxPlayers, 128), new int[maxPlayers]);

            // Act
            InterestManagementSystem.Execute(currentData, interestMask, activePlayers, sparseToDense, relevantEntities);

            // Assert - all 32 players should see the entity
            for (var p = 0; p < maxPlayers; p++)
            {
                Assert.That(relevantEntities.RowCount(p), Is.EqualTo(1),
                    $"Player {p} should see exactly 1 entity.");
                Assert.That(relevantEntities[p, 0], Is.EqualTo(0),
                    $"Player {p} should see dense index 0.");
            }
        }
    }
}
