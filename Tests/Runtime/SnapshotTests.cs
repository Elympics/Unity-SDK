using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Elympics.Tests
{
    [TestFixture]
    public class SnapshotTests
    {
        [TestCase(new int[] { 1, 3, 4, 7 }, new int[] { 1, 2, 3, 4, 5, 6, 7 })]
        [TestCase(new int[] { 1, 2, 3, 4, 5, 6, 7 }, new int[] { 1, 2, 3, 4, 5, 6, 7 })]
        [TestCase(new int[] { 7 }, new int[] { 1, 7 })]
        [TestCase(new int[] { 7 }, new int[] { 7 })]
        [TestCase(new int[] { 1, 2, 3, 4, 5, 6 }, new int[] { 1, 2, 3, 4, 5, 7 })]
        [TestCase(new int[] { 1, 2, 3, 4, 5, 6, 7 }, new int[] { 1, 2, 3, 4, 5, 7 })]
        public void FillMissingFrom(int[] initialNetworkIDs, int[] newNetworkIDs)
        {
            var initialData = new List<KeyValuePair<int, byte[]>>();

            //Set up "server" snapshot that might not contain all objects
            foreach (var networkId in initialNetworkIDs)
                initialData.Add(new KeyValuePair<int, byte[]>(networkId, new byte[0]));

            var snapshot = new ElympicsSnapshot() { Data = initialData };
            var source = new ElympicsSnapshot() { Data = new() };

            //Set up "local" snapshot that has data of all objects
            foreach (var networkId in newNetworkIDs)
                source.Data.Add(new KeyValuePair<int, byte[]>(networkId, new byte[0]));

            //Get missing data from "local" snapshot
            snapshot.FillMissingFrom(source);


            var expected = new HashSet<int>(initialNetworkIDs);

            foreach (var networkId in newNetworkIDs)
            {
                _ = expected.Add(networkId);
            }

            CollectionAssert.AreEquivalent(expected, snapshot.Data.Select(kvp => kvp.Key));
        }
    }
}
