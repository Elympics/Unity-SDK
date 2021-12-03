using NUnit.Framework;
using System.IO;
using System;
using Elympics;

namespace Tests
{
	public class TestInputWriting
	{
		private IBinaryInputWriter inputWriter;

		[SetUp]
		public void SetUp()
		{
			inputWriter = new BinaryInputWriter();
		}

		[TearDown]
		public void TearDown()
		{
			((BinaryInputWriter) inputWriter).Dispose();
		}

		[Test]
		public void TestSerializeOneValue()
		{
			const int x = 42;
			inputWriter.Write(x);
			byte[] serializedData = inputWriter.GetData();
			Assert.AreEqual(sizeof(int), serializedData.Length);
			using (var ms = new MemoryStream(serializedData))
			using (var br = new BinaryReader(ms))
			{
				var xDeserialized = br.ReadInt32();
				Assert.AreEqual(x, xDeserialized);
			}
		}

		[Test]
		public void TestSerializeMultipleValues()
		{
			const int x = 42;
			const string s = "asd";
			inputWriter.Write(x);
			inputWriter.Write(s);
			byte[] serializedData = inputWriter.GetData();
			Assert.AreEqual(sizeof(int) + (s.Length + 1), serializedData.Length);
			using (var ms = new MemoryStream(serializedData))
			using (var br = new BinaryReader(ms))
			{
				var xDeserialized = br.ReadInt32();
				Assert.AreEqual(x, xDeserialized);
				var sDeserialized = br.ReadString();
				Assert.AreEqual(s, sDeserialized);
			}
		}

		[Test]
		public void TestSerializeArray()
		{
			byte[] bytes = new byte[] { 2, 1, 3, 7 };
			inputWriter.Write(bytes);
			byte[] serializedData = inputWriter.GetData();
			Assert.AreEqual(sizeof(byte) * bytes.Length, serializedData.Length);
			using (var ms = new MemoryStream(serializedData))
			using (var br = new BinaryReader(ms))
			{
				var bytesDeserialized = br.ReadBytes(bytes.Length);
				Assert.AreEqual(bytes, bytesDeserialized);
			}
		}

		[Test]
		public void TestSerializeConsecutiveParts()
		{
			const int x = 4;
			const int y = 2;
			inputWriter.Write(x);
			var data1 = inputWriter.GetData();
			Assert.AreEqual(sizeof(int), data1.Length);
			inputWriter.ResetStream();
			inputWriter.Write(y);
			var data2 = inputWriter.GetData();
			Assert.AreEqual(sizeof(int), data2.Length);
			using (var ms = new MemoryStream(data1))
			using (var br = new BinaryReader(ms))
			{
				var xDeserialized = br.ReadInt32();
				Assert.AreEqual(x, xDeserialized);
				ms.SetLength(0);
				ms.Write(data2, 0, data2.Length);
				ms.Seek(0, SeekOrigin.Begin);
				var yDeserialized = br.ReadInt32();
				Assert.AreEqual(y, yDeserialized);
			}
		}
	}
}