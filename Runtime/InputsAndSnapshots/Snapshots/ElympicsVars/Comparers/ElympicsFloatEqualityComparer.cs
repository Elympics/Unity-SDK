using System;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsFloatEqualityComparer : ElympicsVarEqualityComparer<float>
	{
		public ElympicsFloatEqualityComparer()
		{
		}

		public ElympicsFloatEqualityComparer(float initialTolerance) : base(initialTolerance)
		{
		}

		protected override float Distance(float a, float b) => Mathf.Abs(a - b);
	}
}
