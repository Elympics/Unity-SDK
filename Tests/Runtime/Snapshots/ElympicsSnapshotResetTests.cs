using System.Collections.Generic;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Snapshots
{
    [TestFixture]
    [Category("ElympicsSnapshot")]
    public class ElympicsSnapshotResetTests
    {
        [Test]
        public void ResetToEmpty_ClearsAllFields()
        {
            // Arrange
            var data = new Dictionary<int, byte[]> { { 1, new byte[] { 1, 2, 3 } } };
            var inputData = new Dictionary<int, TickToPlayerInput> { { 0, default } };
            var snapshot = new ElympicsSnapshot(
                tick: 42,
                tickStartUtc: System.DateTime.UtcNow,
                factory: new FactoryState(new Dictionary<int, FactoryPartState>
                {
                    { 0, new FactoryPartState(0, new DynamicElympicsBehaviourInstancesDataState(0, new Dictionary<int, DynamicElympicsBehaviourInstanceData>())) }
                }),
                data: data,
                tickToPlayersInputData: inputData);

            // Act
            snapshot.ResetToEmpty();

            // Assert
            Assert.That(snapshot.Tick, Is.EqualTo(-1));
            Assert.That(snapshot.TickStartUtc, Is.EqualTo(default(System.DateTime)));
            Assert.That(snapshot.Data.Count, Is.EqualTo(0));
            Assert.That(snapshot.Factory.Parts.Count, Is.EqualTo(0));
            Assert.That(snapshot.TickToPlayersInputData.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResetToEmpty_NullDataSafe()
        {
            // Arrange
            var snapshot = new ElympicsSnapshot(
                tick: 10,
                tickStartUtc: System.DateTime.UtcNow,
                factory: new FactoryState(new Dictionary<int, FactoryPartState>()),
                data: null,
                tickToPlayersInputData: null);

            // Act & Assert — must not throw
            Assert.DoesNotThrow(() => snapshot.ResetToEmpty());
            Assert.That(snapshot.Tick, Is.EqualTo(-1));
            Assert.That(snapshot.Data, Is.Null);
            Assert.That(snapshot.TickToPlayersInputData, Is.Null);
        }
    }
}
