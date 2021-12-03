using System;

namespace Elympics
{
	public interface IInputWriter
	{
		void Write(bool value);
		void Write(byte value);
		void Write(byte[] value);
		void Write(char value);
		void Write(char[] value);
		void Write(decimal value);
		void Write(double value);
		void Write(float value);
		void Write(int value);
		void Write(long value);
		void Write(sbyte value);
		void Write(short value);
		void Write(string value);
		void Write(uint value);
		void Write(ulong value);
		void Write(ushort value);
		void Write<TEnum>(TEnum value) where TEnum : Enum;
	}
}
