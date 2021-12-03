using System.IO;

namespace Elympics
{
	internal static class ElympicsSnapshotSerializer
	{
		internal static byte[] Serialize(this ElympicsSnapshot elympicsSnapshot)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				elympicsSnapshot.Serialize(bw);
				return ms.ToArray();
			}
		}

		internal static void Serialize(this ElympicsSnapshot elympicsSnapshot, BinaryWriter bw)
		{
			bw.Write(elympicsSnapshot.Tick);
			elympicsSnapshot.Factory.Serialize(bw);
			bw.Write(elympicsSnapshot.Data);
		}

		internal static ElympicsSnapshot Deserialize(byte[] data)
		{
			var elympicsSnapshot = new ElympicsSnapshot();
			elympicsSnapshot.Deserialize(data);
			return elympicsSnapshot;
		}

		internal static void Deserialize(this ElympicsSnapshot elympicsSnapshot, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				elympicsSnapshot.Deserialize(br);
		}

		internal static void Deserialize(this ElympicsSnapshot elympicsSnapshot, BinaryReader br)
		{
			elympicsSnapshot.Tick = br.ReadInt64();
			elympicsSnapshot.Factory = FactoryStateSerializer.Deserialize(br);
			elympicsSnapshot.Data = br.ReadListWithKvpIntToByteArray();
		}
	}
}
