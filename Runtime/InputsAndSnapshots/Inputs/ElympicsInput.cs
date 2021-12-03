using System.Collections.Generic;

namespace Elympics
{
	public class ElympicsInput : ElympicsDataWithTick
	{
		public override long                            Tick   { get; set; }
		public          ElympicsPlayer                  Player { get; set; }
		public          List<KeyValuePair<int, byte[]>> Data   { get; set; }

		public static readonly ElympicsInput Empty = new ElympicsInput
		{
			Data = new List<KeyValuePair<int, byte[]>>()
		};
	}
}
