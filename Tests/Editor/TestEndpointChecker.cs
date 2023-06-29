using System.Collections.Generic;
using Elympics;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
	public class TestEndpointChecker
	{
		private EditorEndpointChecker _endpointChecker;

		private static IEnumerable<TestAddress> _testAddresses => new List<TestAddress>()
		{
			new TestAddress("https", false),
			new TestAddress("https:", false),
			new TestAddress("https://", false),
			new TestAddress("https://test", true),
			new TestAddress("https://test.domain/", true),
			new TestAddress("https://test.domain/path", true),
			new TestAddress("https://test.domain/path/", true)
		};

		[SetUp]
		public void SetUp()
		{
			_endpointChecker = new EditorEndpointChecker();
		}

		[Test]
		public void TestUrlValidation([ValueSource(nameof(_testAddresses))] TestAddress testAddress)
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
