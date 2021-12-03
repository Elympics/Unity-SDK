using System;
using System.IO;

namespace Elympics
{
	internal class BinaryInputWriter : BinaryWriter, IBinaryInputWriter, IDisposable
	{
		private readonly MemoryStream memoryStream;

		public BinaryInputWriter() : base(new MemoryStream())
		{
			memoryStream = (MemoryStream) OutStream;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
				memoryStream.Dispose();
		}

		public void ResetStream()
		{
			memoryStream.SetLength(0);
		}

		public byte[] GetData()
		{
			if (memoryStream.Length == 0)
				return null; // avoid unnecessary allocation
			return memoryStream.ToArray();
		}

		public void Write<TEnum>(TEnum value) where TEnum : Enum => base.Write((int) (object) value);
	}
}
