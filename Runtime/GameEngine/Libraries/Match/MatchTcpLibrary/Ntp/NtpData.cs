using System;

namespace MatchTcpLibrary.Ntp
{
    // Class implementation based on standard https://tools.ietf.org/html/rfc4330
    public class NtpData
    {
        public const int NtpDataLength = 32;
        public byte[] Data { get; } = new byte[NtpDataLength];

        private const int OffReferenceTimestamp = 0;
        private const int OffOriginateTimestamp = 8;
        private const int OffReceiveTimestamp = 16;
        private const int OffTransmitTimestamp = 24;

        public void SetFromBytes(byte[] data) => Array.Copy(data, Data, NtpDataLength);

        public void Clear()
        {
            for (var i = 0; i < Data.Length; i++)
                Data[i] = 0;
        }

        // This field is the time the system clock was last set or corrected, in 64-bit timestamp format.
        public DateTime ReferenceTimestamp
        {
            get => NtpUtils.NtpDataTimeStampToDateTime(Data, OffReferenceTimestamp);
            set => NtpUtils.DateTimeToNtpDataTimeStamp(value, Data, OffReferenceTimestamp);
        }

        // This is the time at which the request departed the client for the server, in 64-bit timestamp format.
        public DateTime OriginateTimestamp
        {
            get => NtpUtils.NtpDataTimeStampToDateTime(Data, OffOriginateTimestamp);
            set => NtpUtils.DateTimeToNtpDataTimeStamp(value, Data, OffOriginateTimestamp);
        }

        // This is the time at which the request arrived at the server, in 64-bit timestamp format.
        public DateTime ReceiveTimestamp
        {
            get => NtpUtils.NtpDataTimeStampToDateTime(Data, OffReceiveTimestamp);
            set => NtpUtils.DateTimeToNtpDataTimeStamp(value, Data, OffReceiveTimestamp);
        }

        // This is the time at which the request departed the client or the reply departed the server, in 64-bit timestamp format.
        public DateTime TransmitTimestamp
        {
            get => NtpUtils.NtpDataTimeStampToDateTime(Data, OffTransmitTimestamp);
            set => NtpUtils.DateTimeToNtpDataTimeStamp(value, Data, OffTransmitTimestamp);
        }

        // This is the time at which the reply arrived at the client, in 64-bit timestamp format.
        public DateTime ReceptionTimestamp;

        public TimeSpan RoundTripDelay =>
            ReceiveTimestamp - OriginateTimestamp + (ReceptionTimestamp - TransmitTimestamp);

        public TimeSpan LocalClockOffset =>
            TimeSpan.FromTicks((ReceiveTimestamp - OriginateTimestamp - (ReceptionTimestamp - TransmitTimestamp)).Ticks / 2);
    }
}
