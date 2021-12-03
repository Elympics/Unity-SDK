using System;

namespace Elympics
{
	[Serializable]
	public abstract class ElympicsVarEqualityComparer<T> : ElympicsVarEqualityComparer
	{
		protected ElympicsVarEqualityComparer(float initialTolerance = 0.01f) : base(initialTolerance)
		{
		}

		public bool Equals(T a, T b) => Distance(a, b) <= Tolerance;

		protected abstract float Distance(T a, T b);
	}

	public abstract class ElympicsVarEqualityComparer
	{
		private float _tolerance;

		public float Tolerance { get => _tolerance; set => _tolerance = Math.Max(value, 0f); }

		protected ElympicsVarEqualityComparer(float initialTolerance) => Tolerance = initialTolerance;
	}
}
