using System;

namespace Elympics
{
	public interface IInputReader
	{
		bool ReadBoolean();
		byte ReadByte();
		byte[] ReadBytes(int count);
		char ReadChar();
		char[] ReadChars(int count);
		decimal ReadDecimal();
		double ReadDouble();
		short ReadInt16();
		int ReadInt32();
		long ReadInt64();
		sbyte ReadSByte();
		float ReadSingle();
		string ReadString();
		ushort ReadUInt16();
		uint ReadUInt32();
		ulong ReadUInt64();
		TEnum ReadEnumFromInt32<TEnum>() where TEnum : Enum;

		void Read(out bool value);
		void Read(out byte value);
		void Read(out char value);
		void Read(out decimal value);
		void Read(out double value);
		void Read(out short value);
		void Read(out int value);
		void Read(out long value);
		void Read(out sbyte value);
		void Read(out float value);
		void Read(out string value);
		void Read(out ushort value);
		void Read(out uint value);
		void Read(out ulong value);
		void Read<TEnum>(out TEnum value) where TEnum : Enum;
	}
}