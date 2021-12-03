using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	internal static class ElympicsInputSerializer
	{
		internal static byte[] Serialize(this ElympicsInput elympicsInput)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				elympicsInput.Serialize(bw);
				return ms.ToArray();
			}
		}

		internal static void Serialize(this ElympicsInput elympicsInput, BinaryWriter bw)
		{
			bw.Write(elympicsInput.Tick);
			bw.Write((int) elympicsInput.Player);
			bw.Write(elympicsInput.Data);
		}

		internal static ElympicsInput Deserialize(byte[] data)
		{
			var elympicsInput = new ElympicsInput();
			elympicsInput.Deserialize(data);
			return elympicsInput;
		}

		internal static void Deserialize(this ElympicsInput elympicsInput, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				elympicsInput.Deserialize(br);
		}

		internal static void Deserialize(this ElympicsInput elympicsInput, BinaryReader br)
		{
			elympicsInput.Tick = br.ReadInt64();
			elympicsInput.Player = ElympicsPlayer.FromIndex(br.ReadInt32());
			elympicsInput.Data = br.ReadListWithKvpIntToByteArray();
		}

		internal static byte[] SerializePackage(this List<ElympicsInput> elympicsInput)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				elympicsInput.SerializePackage(bw);
				return ms.ToArray();
			}
		}

		internal static void SerializePackage(this List<ElympicsInput> elympicsInputs, BinaryWriter bw)
		{
			foreach (var elympicsInput in elympicsInputs)
			{
				bw.Write(elympicsInput.Tick);
				bw.Write((int) elympicsInput.Player);
				bw.Write(elympicsInput.Data);
			}
		}

		internal static byte[] MergeInputsToPackage(byte[][] serializedInputs)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (var serializedInput in serializedInputs)
					bw.Write(serializedInput);
				return ms.ToArray();
			}
		}

		internal static List<ElympicsInput> DeserializePackage(byte[] data)
		{
			var elympicsInputs = new List<ElympicsInput>();
			elympicsInputs.DeserializePackage(data);
			return elympicsInputs;
		}

		internal static void DeserializePackage(this List<ElympicsInput> elympicsInput, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				elympicsInput.DeserializePackage(br);
		}

		internal static void DeserializePackage(this List<ElympicsInput> elympicsInputs, BinaryReader br)
		{
			;
			while (br.BaseStream.Position != br.BaseStream.Length)
			{
				var elympicsInput = new ElympicsInput();
				elympicsInput.Deserialize(br);
				elympicsInputs.Add(elympicsInput);
			}
		}
	}
}
