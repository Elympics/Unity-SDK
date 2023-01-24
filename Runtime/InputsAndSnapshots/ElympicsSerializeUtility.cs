using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	internal static class ElympicsSerializeUtility
	{
		internal static byte[] Serialize(this IElympicsSerializable serializable)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				serializable.Serialize(bw);
				return ms.ToArray();
			}
		}

		internal static void Serialize(this IElympicsSerializable serializable, BinaryWriter bw)
		{
			serializable.Serialize(bw);
		}

		internal static T Deserialize<T>(this byte[] data) where T : IElympicsSerializable, new()
		{
			T toReturn = new T();
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				toReturn.Deserialize(br);
			return toReturn;
		}

		internal static T Deserialize<T>(this BinaryReader br) where T : IElympicsSerializable, new()
		{
			T toReturn = new T();
			toReturn.Deserialize(br);
			return toReturn;
		}

		internal static List<T> DeserializeList<T>(this byte[] data) where T : IElympicsSerializable, new()
		{
			var toReturn = new List<T>();
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
			{
				while (br.BaseStream.Position != br.BaseStream.Length)
				{
					T deserialized = br.Deserialize<T>();
					toReturn.Add(deserialized);
				}
			}
			return toReturn;
		}

		internal static byte[] MergeBytePackage(this byte[][] package)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (var serializedInput in package)
					bw.Write(serializedInput);
				return ms.ToArray();
			}
		}
	}
}