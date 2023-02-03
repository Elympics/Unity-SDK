using System.IO;

namespace Elympics
{
	public static class ClientTickCalculatorNetworkDetailsSerializer
	{
		internal static byte[] Serialize(this ClientTickCalculatorNetworkDetails networkDetails)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				networkDetails.Serialize(bw);
				return ms.ToArray();
			}
		}

		internal static void Serialize(this ClientTickCalculatorNetworkDetails networkDetails, BinaryWriter bw)
		{
			bw.Write(networkDetails.CorrectTicking);
			bw.Write(networkDetails.ForcedTickJump);
			bw.Write(networkDetails.TicksDiffSumBeforeCatchup);
			bw.Write(networkDetails.TickJumpStart);
			bw.Write(networkDetails.TickJumpEnd);
			bw.Write(networkDetails.InputTickJumpStart);
			bw.Write(networkDetails.InputTickJumpEnd);
			bw.Write(networkDetails.InputLagTicks);
			bw.Write(networkDetails.RttTicks);
			bw.Write(networkDetails.LcoTicks);
			bw.Write(networkDetails.CtasTicks);
			bw.Write(networkDetails.StasTicks);
		}

		internal static ClientTickCalculatorNetworkDetails Deserialize(byte[] data)
		{
			var networkDetails = new ClientTickCalculatorNetworkDetails();
			networkDetails.Deserialize(data);
			return networkDetails;
		}

		internal static void Deserialize(this ref ClientTickCalculatorNetworkDetails networkDetails, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				networkDetails.Deserialize(br);
		}

		internal static void Deserialize(this ref ClientTickCalculatorNetworkDetails networkDetails, BinaryReader br)
		{
			networkDetails.CorrectTicking = br.ReadBoolean();
			networkDetails.ForcedTickJump = br.ReadBoolean();
			networkDetails.TicksDiffSumBeforeCatchup = br.ReadInt64();
			networkDetails.TickJumpStart = br.ReadInt64();
			networkDetails.TickJumpEnd = br.ReadInt64();
			networkDetails.InputTickJumpStart = br.ReadInt64();
			networkDetails.InputTickJumpEnd = br.ReadInt64();
			networkDetails.InputLagTicks = br.ReadInt32();
			networkDetails.RttTicks = br.ReadDouble();
			networkDetails.LcoTicks = br.ReadDouble();
			networkDetails.CtasTicks = br.ReadDouble();
			networkDetails.StasTicks = br.ReadDouble();
		}
	}
}
