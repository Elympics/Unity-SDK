using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
		public static void Write(this BinaryWriter bw, Dictionary<string, string> dict)
		{
			bw.Write(dict.Count);
			foreach (var (key, val) in dict)
			{
				bw.Write(key);
				bw.Write(val);
			}
		}

		public static void Write<T>(this BinaryWriter bw, List<T> list, Action<BinaryWriter, T> write)
		{
			if (list == null)
			{
				bw.Write(-1);
				return;
			}
			bw.Write(list.Count);
			foreach (var t in list)
				write(bw, t);
		}
	}
}
