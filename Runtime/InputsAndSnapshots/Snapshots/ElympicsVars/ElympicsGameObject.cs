using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsGameObject : ElympicsVar<ElympicsBehaviour>
	{
		private const int nullReferenceNetworkId = -1;

		public ElympicsGameObject(ElympicsBehaviour value = default, bool enableSynchronization = true)
			: base(value, enableSynchronization)
		{
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(Value != null ? Value.networkId : nullReferenceNetworkId);
		}

		protected override ElympicsBehaviour DeserializeInternal(BinaryReader br)
		{
			int valueNetworkId = br.ReadInt32();

			if (valueNetworkId == nullReferenceNetworkId)
			{
				Value = null;
			}
			else if (Value == null || Value.networkId != valueNetworkId)
			{
				if (Elympics.TryGetBehaviour(valueNetworkId, out ElympicsBehaviour elympicsBehaviour))
				{
					Value = elympicsBehaviour;
				}
			}

			return Value;
		}
	}
}
