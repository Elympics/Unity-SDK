namespace MatchTcpLibrary.TransportLayer.SimpleMessageEncoder
{
    public class SimpleMessageEncoderConfig
    {
        public byte Delimiter { get; private set; }
        public int DelimiterRepeated { get; private set; }

        //TODO: TO JEST TAKA KURLA PULAPKA, DZIEKI EMI ~emi 10.02.2020
        public static SimpleMessageEncoderConfig Default => new()
        {
            Delimiter = (byte)'|',
            DelimiterRepeated = 3
        };
    }
}
