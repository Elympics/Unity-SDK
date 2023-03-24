using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Elympics.EgbTest
{
	public static class DataConverter
	{
		public static byte[] ParseGameEngineData(string geData) => string.IsNullOrEmpty(geData)
			? null
			: Convert.FromBase64String(geData);

		public static float[] ParseMatchmakerData(string mmData) => string.IsNullOrEmpty(mmData.Trim('[', ']', ' '))
			? null
			: mmData.Trim('[', ']', ' ').Split(',').Select(x => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();

		public static string StringifyGameEngineData(byte[] geData) => geData == null
			? null
			: Convert.ToBase64String(geData);

		public static string StringifyMatchmakerData(IEnumerable<float> mmData) => mmData == null
			? null
			: $"[{string.Join(", ", mmData.Select(x => $"{x}"))}]";
	}
}
