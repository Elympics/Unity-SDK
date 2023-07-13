using System;

namespace Elympics
{
    public class ElympicsDefaultEqualityComparer<T> : ElympicsVarEqualityComparer<T>
        where T : IEquatable<T>
    {
        public ElympicsDefaultEqualityComparer()
        {
        }

        public ElympicsDefaultEqualityComparer(float initialTolerance) : base(initialTolerance)
        {
        }

        protected override float Distance(T a, T b) => object.Equals(a, b) ? 0f : float.PositiveInfinity;
    }
}
