using System;
using System.IO;

namespace Elympics
{
	[Serializable]
	public sealed class ElympicsString : ElympicsVar<string>
	{
		public ElympicsString() : this(string.Empty)
		{
		}

		public ElympicsString(string value, bool enableSynchronization = true) : base(value, enableSynchronization)
		{
		}

		public ElympicsString(string value, ElympicsVarConfig config) : base(value, config.synchronizationEnabled)
		{
			Comparer.Tolerance = config.tolerance;
		}

		public override    void   Serialize(BinaryWriter bw)           => bw.Write(Value);
		protected override string DeserializeInternal(BinaryReader br) => br.ReadString();
	}
}
