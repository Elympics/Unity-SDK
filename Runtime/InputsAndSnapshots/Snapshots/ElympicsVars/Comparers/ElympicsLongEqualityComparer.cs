using System;

namespace Elympics
{
    [Serializable]
    public class ElympicsLongEqualityComparer : ElympicsVarEqualityComparer<long>
    {
        public ElympicsLongEqualityComparer(float tolerance = 0f) : base(tolerance) { }

        protected override float Distance(long a, long b) => Math.Abs(a - b);
    }
}
