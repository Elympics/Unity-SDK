using System;
using System.Collections.Generic;

#nullable enable

namespace Elympics.Util
{
    internal class StringIReadOnlyDictionaryEqualityComparer : IEqualityComparer<IReadOnlyDictionary<string, string>?>
    {
        private static StringIReadOnlyDictionaryEqualityComparer? instance;
        public static StringIReadOnlyDictionaryEqualityComparer Instance => instance ??= new StringIReadOnlyDictionaryEqualityComparer();

        public bool Equals(IReadOnlyDictionary<string, string>? x, IReadOnlyDictionary<string, string>? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            if (x.Count != y.Count)
                return false;
            foreach (var (xKey, xValue) in x)
                if (!y.TryGetValue(xKey, out var yValue) || !yValue.Equals(xValue))
                    return false;
            return true;
        }

        public int GetHashCode(IReadOnlyDictionary<string, string>? obj) => HashCode.Combine(obj?.Count, obj?.Keys, obj?.Values);
    }
}
