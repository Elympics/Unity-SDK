using Elympics;
using NUnit.Framework;
using System.IO;

namespace Elympics.Tests
{
	public class TestInputReading
	{
		private BinaryInputReader _inputReader;
		private MemoryStream _memoryStream;
		private BinaryWriter _binaryWriter;

		[SetUp]
		public void SetUp()
		{
			_inputReader = new BinaryInputReader();
			_memoryStream = new MemoryStream();
			_binaryWriter = new BinaryWriter(_memoryStream);
		}

		[TearDown]
		public void TearDown()
		{
			_inputReader.Dispose();
			_memoryStream.Dispose();
			_binaryWriter.Dispose();
		}

		[Test]
		public void TestDeserializeOneValueValid()
		{
			const int x = 42;
			_binaryWriter.Write(x);

			_inputReader.FeedDataForReading(_memoryStream.ToArray());
			var xDeserialized = _inputReader.ReadInt32();
			Assert.AreEqual(x, xDeserialized);
			Assert.IsTrue(_inputReader.AllBytesRead());
		}

		[Test]
		public void TestDeserializeOneValueTooMuch()
		{
			const int x = 42;
			_binaryWriter.Write(x);

			_inputReader.FeedDataForReading(_memoryStream.ToArray());
			var xDeserialized = _inputReader.ReadInt32();
			Assert.Throws<EndOfStreamException>(() => _inputReader.ReadInt32());
		}

		[Test]
		public void TestDeserializeTooManyBytes()
		{
			_inputReader.FeedDataForReading(new byte[] { 0 });
			Assert.Throws<EndOfStreamException>(() => _inputReader.ReadInt32());
		}

		[Test]
		public void TestDeserializeOneValueNotEnough()
		{
			_inputReader.FeedDataForReading(new byte[] { 4, 2 });
			Assert.IsFalse(_inputReader.AllBytesRead());
		}
	}
}
