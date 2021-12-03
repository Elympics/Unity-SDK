using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	public static class BinaryWriterExtensions
	{
		public static void Write(this BinaryWriter bw, ICollection<KeyValuePair<int, byte[]>> dict)
		{
			bw.Write(dict.Count);
			foreach (var (id, data) in dict)
			{
				bw.Write(id);
				bw.Write(data.Length);
				bw.Write(data);
			}
		}

		public static Dictionary<int, byte[]> ReadDictionaryIntToByteArray(this BinaryReader br)
		{
			var dataDictCount = br.ReadInt32();
			var dict = new Dictionary<int, byte[]>(dataDictCount);
			for (var i = 0; i < dataDictCount; i++)
			{
				var id = br.ReadInt32();
				var dataLength = br.ReadInt32();
				var data = br.ReadBytes(dataLength);
				dict.Add(id, data);
			}

			return dict;
		}

		public static List<KeyValuePair<int, byte[]>> ReadListWithKvpIntToByteArray(this BinaryReader br)
		{
			var dataListCount = br.ReadInt32();
			var list = new List<KeyValuePair<int, byte[]>>(dataListCount);
			for (var i = 0; i < dataListCount; i++)
			{
				var id = br.ReadInt32();
				var dataLength = br.ReadInt32();
				var data = br.ReadBytes(dataLength);
				list.Add(new KeyValuePair<int, byte[]>(id, data));
			}

			return list;
		}

		public static void ReadIntoDictionaryIntToByteArray(this BinaryReader br, Dictionary<int, byte[]> dict)
		{
			var dataDictCount = br.ReadInt32();
			for (var i = 0; i < dataDictCount; i++)
			{
				var id = br.ReadInt32();
				var dataLength = br.ReadInt32();
				var data = br.ReadBytes(dataLength);
				dict.Add(id, data);
			}
		}
	}
}
