using System;

namespace Elympics
{
	[Serializable]
	public class ElympicsIntEqualityComparer : ElympicsVarEqualityComparer<int>
	{
		public ElympicsIntEqualityComparer(float tolerance = 0f) : base(tolerance)
		{
		}

		protected override float Distance(int a, int b) => Math.Abs((long)a - b);
	}
}
