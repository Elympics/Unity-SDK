using System;
using System.IO;

namespace Elympics
{
    internal sealed class BinaryInputReader : BinaryReader, IBinaryInputReader
    {
        private readonly MemoryStream _memoryStream;

        public BinaryInputReader() : base(new MemoryStream()) => _memoryStream = (MemoryStream)BaseStream;

        public void FeedDataForReading(byte[] data)
        {
            _memoryStream.SetLength(0);
            _memoryStream.Write(data, 0, data.Length);
            _ = _memoryStream.Seek(0, SeekOrigin.Begin);
        }

        public bool AllBytesRead()
            => _memoryStream.Position == _memoryStream.Length;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _memoryStream.Dispose();
        }

        public TEnum ReadEnumFromInt32<TEnum>() where TEnum : Enum
            => (TEnum)(object)ReadInt32();

        #region public void Read(out value)
        public void Read(out bool value) => value = ReadBoolean();
        public void Read(out byte value) => value = ReadByte();
        public void Read(out char value) => value = ReadChar();
        public void Read(out decimal value) => value = ReadDecimal();
        public void Read(out double value) => value = ReadDouble();
        public void Read(out short value) => value = ReadInt16();
        public void Read(out int value) => value = ReadInt32();
        public void Read(out long value) => value = ReadInt64();
        public void Read(out sbyte value) => value = ReadSByte();
        public void Read(out float value) => value = ReadSingle();
        public void Read(out string value) => value = ReadString();
        public void Read(out ushort value) => value = ReadUInt16();
        public void Read(out uint value) => value = ReadUInt32();
        public void Read(out ulong value) => value = ReadUInt64();
        public void Read<TEnum>(out TEnum value) where TEnum : Enum => value = ReadEnumFromInt32<TEnum>();
        #endregion
    }
}
