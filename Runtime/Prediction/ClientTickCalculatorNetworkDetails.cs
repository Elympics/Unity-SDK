using System.Text;

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
		public double ExactTickCalculated;
		public long   Diff;

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine("###Tick Calculation Summary###");
			if (ForcedTickJump)
				sb.AppendLine($"Client was forced to jump onto tick {TickJumpEnd}");
			sb.AppendLine($"Exact Tick Calculated {ExactTickCalculated}");
			sb.AppendLine($"Diff between exactTick and expectations {Diff}");
			sb.AppendLine($"Client input from {InputTickJumpStart} to {InputTickJumpEnd}");
			sb.AppendLine($"Client predicted {TickJumpEnd - TickJumpStart + 1} ticks from {TickJumpStart} to {TickJumpEnd}");
			sb.AppendLine("###Network conditions###");
			sb.AppendLine($"Input lag - {InputLagTicks:F} ticks");
			sb.AppendLine($"Round trip time - {RttTicks:F} ticks");
			sb.AppendLine($"Local clock offset - {LcoTicks:F} ticks");
			sb.AppendLine($"Client ticking aberration sum - {CtasTicks:F} ticks");
			sb.AppendLine($"Server ticking aberration sum - {StasTicks:F} ticks");

			return sb.ToString();
		}
	}
}