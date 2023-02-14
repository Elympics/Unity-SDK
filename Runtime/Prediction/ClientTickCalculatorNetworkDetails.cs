namespace Elympics
{
	public struct ClientTickCalculatorNetworkDetails
	{
		public bool   CorrectTicking;
		public bool   ForcedTickJump;
		public long   TicksDiffSumBeforeCatchup;
		public long   TickJumpStart;
		public long   TickJumpEnd;
		public long   InputTickJumpStart;
		public long   InputTickJumpEnd;
		public int    InputLagTicks;
		public double RttTicks;
		public double LcoTicks;
		public double CtasTicks;
		public double StasTicks;

		public override string ToString()
		{
			return $"Client {(ForcedTickJump ? "forced " : "")}tick jump with difference {TickJumpEnd - TickJumpStart} from {TickJumpStart} to {TickJumpEnd}, inputs from {InputTickJumpStart} to {InputTickJumpEnd}\n" +
			       $"Network conditions:\n" +
			       $"Input lag - {InputLagTicks:F} ticks\n" +
			       $"Round trip time - {RttTicks:F} ticks\n" +
			       $"Local clock offset - {LcoTicks:F} ticks\n" +
			       $"Client ticking aberration sum - {CtasTicks:F} ticks\n" +
			       $"Server ticking aberration sum - {StasTicks:F} ticks";
		}
	}
}
