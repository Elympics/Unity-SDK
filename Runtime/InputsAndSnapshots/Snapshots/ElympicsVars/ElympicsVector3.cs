using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public sealed class ElympicsVector3 : ElympicsVar<Vector3>
	{
		public ElympicsVector3(Vector3 value = default, bool enableSynchronization = true, ElympicsVector3EqualityComparer comparer = null)
			: base(value, enableSynchronization, comparer ?? new ElympicsVector3EqualityComparer())
		{
		}

		public ElympicsVector3(Vector3 value, ElympicsVarConfig config)
			: base(value, config.synchronizationEnabled, new ElympicsVector3EqualityComparer(config.tolerance))
		{
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value.x);
			bw.Write(Value.y);
			bw.Write(Value.z);
		}

		protected override Vector3 DeserializeInternal(BinaryReader br) => new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
	}
}
