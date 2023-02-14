using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public sealed class ElympicsVector2 : ElympicsVar<Vector2>
	{
		public ElympicsVector2(Vector2 value = default, bool enableSynchronization = true, ElympicsVector2EqualityComparer comparer = null)
			: base(value, enableSynchronization, comparer ?? new ElympicsVector2EqualityComparer())
		{
		}

		public ElympicsVector2(Vector2 value, ElympicsVarConfig config)
			: base(value, config.synchronizationEnabled, new ElympicsVector2EqualityComparer(config.tolerance))
		{
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value.x);
			bw.Write(Value.y);
		}

		protected override Vector2 DeserializeInternal(BinaryReader br) => new Vector2(br.ReadSingle(), br.ReadSingle());

		public override string ToString() => Value.ToString("G");
	}
}
