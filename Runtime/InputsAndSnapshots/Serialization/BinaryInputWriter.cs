using System;
using System.IO;

namespace Elympics
{
    internal class BinaryInputWriter : BinaryWriter, IBinaryInputWriter, IDisposable
    {
        private readonly MemoryStream _memoryStream;

        public BinaryInputWriter() : base(new MemoryStream())
        {
            _memoryStream = (MemoryStream)OutStream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _memoryStream.Dispose();
        }

        public void ResetStream()
        {
            _memoryStream.SetLength(0);
        }

        public byte[] GetData()
        {
            if (_memoryStream.Length == 0)
                return null; // avoid unnecessary allocation
            return _memoryStream.ToArray();
        }

        public void Write<TEnum>(TEnum value) where TEnum : Enum => base.Write((int)(object)value);
    }
}
