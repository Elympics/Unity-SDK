using System;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public class ElympicsVector2EqualityComparer : ElympicsVarEqualityComparer<Vector2>
    {
        public ElympicsVector2EqualityComparer()
        {
        }

        public ElympicsVector2EqualityComparer(float initialTolerance) : base(initialTolerance)
        {
        }

        protected override float Distance(Vector2 a, Vector2 b) => Vector2.Distance(a, b);
    }
}
