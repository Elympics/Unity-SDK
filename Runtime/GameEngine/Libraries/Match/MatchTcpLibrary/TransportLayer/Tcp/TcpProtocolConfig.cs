namespace MatchTcpLibrary.TransportLayer.Tcp
{
	public class TcpProtocolConfig
	{
		public int MaxConnectionAttempts                 { get; set; }
		public int IntervalBetweenConnectionAttemptsInMs { get; set; }
		public int ReceiveBufferSize                     { get; set; }
		public int ConnectTimeoutMs                      { get; set; }

		public static TcpProtocolConfig Default => new TcpProtocolConfig
		{
			MaxConnectionAttempts = 15,
			IntervalBetweenConnectionAttemptsInMs = 500,
			ReceiveBufferSize = 1024,
			ConnectTimeoutMs = 2000
		};
	}
}
