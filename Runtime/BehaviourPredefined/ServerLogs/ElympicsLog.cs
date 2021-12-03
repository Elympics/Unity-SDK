using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsLog : ElympicsVar<(LogType, string)>
	{
		public ElympicsLog() : base((LogType.Log, string.Empty))
		{
		}

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
			=> Value = DeserializeInternal(br);

		protected override (LogType, string) DeserializeInternal(BinaryReader br)
		{
			var type = (LogType)br.ReadInt32();
			var message = br.ReadString();
			return (type, message);
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write((int)Value.Item1);
			bw.Write(Value.Item2);
		}
	}
}
