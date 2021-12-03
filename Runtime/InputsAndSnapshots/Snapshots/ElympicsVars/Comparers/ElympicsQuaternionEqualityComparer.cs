using System;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsQuaternionEqualityComparer : ElympicsVarEqualityComparer<Quaternion>
	{
		public ElympicsQuaternionEqualityComparer()
		{
		}

		public ElympicsQuaternionEqualityComparer(float initialTolerance) : base(initialTolerance)
		{
		}

		protected override float Distance(Quaternion a, Quaternion b) => Math.Abs(Quaternion.Angle(a, b));
	}
}
