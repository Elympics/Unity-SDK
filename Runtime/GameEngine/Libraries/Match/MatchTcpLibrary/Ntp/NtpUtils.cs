using System;

namespace MatchTcpLibrary.Ntp
{
	public class NtpUtils
	{
		private static readonly DateTime NtpStartDate              = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private const           int      ByteSize                  = 0x100;
		private const           int      TimestampSecondsDataSize  = 4;
		private const           int      TimestampFractionDataSize = 4;
		private const           long     TimestampFractionSize     = 0x100000000L;

		private const int   FixedSecondsDataSize     = 4;
		private const ulong FixedSecondsFractionSize = 0x10000;

		public static DateTime NtpDataTimeStampToDateTime(byte[] data, int offset = 0)
		{
			var seconds  = NtpDataBytesToNumber(data, offset, TimestampSecondsDataSize);
			var fraction = NtpDataBytesToNumber(data, offset + TimestampSecondsDataSize, TimestampFractionDataSize);

			var ticks = seconds * TimeSpan.TicksPerSecond + fraction * TimeSpan.TicksPerSecond / TimestampFractionSize;

			return NtpStartDate.AddTicks((long) ticks);
		}

		public static void DateTimeToNtpDataTimeStamp(DateTime date, byte[] data, int offset = 0)
		{
			var ticks = (date - NtpStartDate).Ticks;

			var seconds  = ticks / TimeSpan.TicksPerSecond;
			var fraction = ticks % TimeSpan.TicksPerSecond * TimestampFractionSize / TimeSpan.TicksPerSecond;

			NumberToNtpDataBytes(data, (ulong) seconds, offset, TimestampSecondsDataSize);
			NumberToNtpDataBytes(data, (ulong) fraction, offset + TimestampSecondsDataSize, TimestampFractionDataSize);
		}

		public static double NtpDataToFixedSeconds(byte[] data, int offset = 0)
		{
			return (double) NtpDataBytesToNumber(data, offset, FixedSecondsDataSize) / FixedSecondsFractionSize;
		}

		public static void FixedSecondsToNtpData(double fixedSeconds, byte[] data, int offset = 0)
		{
			NumberToNtpDataBytes(data, (ulong) (fixedSeconds * FixedSecondsFractionSize), offset, FixedSecondsDataSize);
		}

		public static ulong NtpDataBytesToNumber(byte[] data, int offset, int numOfBytes)
		{
			ulong val = 0;
			for (var i = 0; i < numOfBytes; i++)
				val = val * ByteSize + data[offset + i];

			return val;
		}

		public static void NumberToNtpDataBytes(byte[] data, ulong number, int offset, int numOfBytes)
		{
			for (var i = numOfBytes - 1; i >= 0; i--)
			{
				data[offset + i] = (byte) (number % ByteSize);
				number /= ByteSize;
			}
		}
	}
}
