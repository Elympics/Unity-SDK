using System;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsVector3EqualityComparer : ElympicsVarEqualityComparer<Vector3>
	{
		public ElympicsVector3EqualityComparer()
		{
		}

		public ElympicsVector3EqualityComparer(float initialTolerance) : base(initialTolerance)
		{
		}

		protected override float Distance(Vector3 a, Vector3 b) => Vector3.SqrMagnitude(a - b);
	}
}
