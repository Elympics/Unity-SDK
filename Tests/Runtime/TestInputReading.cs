using Elympics;
using NUnit.Framework;
using System.IO;

namespace Tests
{
	public class TestInputReading
	{
		private BinaryInputReader inputReader;
		private MemoryStream memoryStream;
		private BinaryWriter binaryWriter;

		[SetUp]
		public void SetUp()
		{
			inputReader = new BinaryInputReader();
			memoryStream = new MemoryStream();
			binaryWriter = new BinaryWriter(memoryStream);
		}

		[TearDown]
		public void TearDown()
		{
			inputReader.Dispose();
			memoryStream.Dispose();
			binaryWriter.Dispose();
		}

		[Test]
		public void TestDeserializeOneValueValid()
		{
			int x = 42;
			binaryWriter.Write(x);

			inputReader.FeedDataForReading(memoryStream.ToArray());
			var xDeserialized = inputReader.ReadInt32();
			Assert.AreEqual(x, xDeserialized);
			Assert.IsTrue(inputReader.AllBytesRead());
		}

		[Test]
		public void TestDeserializeOneValueTooMuch()
		{
			int x = 42;
			binaryWriter.Write(x);

			inputReader.FeedDataForReading(memoryStream.ToArray());
			var xDeserialized = inputReader.ReadInt32();
			Assert.Throws<EndOfStreamException>(() => inputReader.ReadInt32());
		}

		[Test]
		public void TestDeserializeTooManyBytes()
		{
			inputReader.FeedDataForReading(new byte[] { 0 });
			Assert.Throws<EndOfStreamException>(() => inputReader.ReadInt32());
		}

		[Test]
		public void TestDeserializeOneValueNotEnough()
		{
			inputReader.FeedDataForReading(new byte[] { 4, 2 });
			Assert.IsFalse(inputReader.AllBytesRead());
		}
	}
}