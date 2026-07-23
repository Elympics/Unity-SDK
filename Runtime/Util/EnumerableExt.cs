#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
    internal static class EnumerableExt
    {
        public static bool SequenceEqualNullable<TSource>(this IEnumerable<TSource>? first, IEnumerable<TSource>? second)
        {
            if (first == null && second == null)
                return true;
            if (first == null || second == null)
                return false;
            return first.SequenceEqual(second);
        }
    }
}
