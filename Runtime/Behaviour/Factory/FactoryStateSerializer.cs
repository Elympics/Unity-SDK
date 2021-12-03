using System.IO;

namespace Elympics
{
	public static class FactoryStateSerializer
	{
		internal static void Serialize(this FactoryState factoryState, BinaryWriter bw)
		{
			bw.Write(factoryState.Parts);
		}

		internal static void Deserialize(this FactoryState factoryState, BinaryReader br)
		{
			factoryState.Parts = br.ReadListWithKvpIntToByteArray();
		}

		internal static FactoryState Deserialize(BinaryReader br)
		{
			var factoryState = new FactoryState();
			factoryState.Deserialize(br);
			return factoryState;
		}

		internal static void Deserialize(this FactoryState factoryState, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				factoryState.Deserialize(br);
		}
	}
}
