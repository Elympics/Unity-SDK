using System.Collections.Generic;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestEndpointChecker
    {
        private EditorEndpointChecker _endpointChecker;

        private static IEnumerable<TestAddress> TestAddresses => new List<TestAddress>()
        {
            new("https", false),
            new("https:", false),
            new("https://", false),
            new("https://test", true),
            new("https://test.domain/", true),
            new("https://test.domain/path", true),
            new("https://test.domain/path/", true)
        };

        [SetUp]
        public void SetUp()
        {
            _endpointChecker = new EditorEndpointChecker();
        }

        [Test]
        public void TestUrlValidation([ValueSource(nameof(TestAddresses))] TestAddress testAddress)
        {
            _endpointChecker.UpdateUri(testAddress.Address);
            Assert.AreEqual(_endpointChecker.IsUriCorrect, testAddress.IsCorrect);
        }

        public class TestAddress
        {
            public string Address;
            public bool IsCorrect;

            public TestAddress(string address, bool isCorrect) => (Address, IsCorrect) = (address, isCorrect);
        }
    }
}
