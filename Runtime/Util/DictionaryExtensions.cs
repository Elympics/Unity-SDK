using System.Collections.Generic;
using System.Text;
using Elympics.Util;

namespace Elympics.Rooms.Models
{
#nullable enable
    internal static class DictionaryExtensions
    {
        public const int MaxDictMemorySize = 1024;

        public static void AddRange<TV, TK>(this Dictionary<TV, TK> target, IReadOnlyDictionary<TV, TK>? source)
        {
            if (source == null)
                return;
            foreach (var (otherKey, otherValue) in source)
                target.Add(otherKey, otherValue);
        }

        public static bool IsTheSame(this IReadOnlyDictionary<string, string>? source, IReadOnlyDictionary<string, string>? target) => StringIReadOnlyDictionaryEqualityComparer.Instance.Equals(source, target);

        public static int GetSizeInBytes(this IReadOnlyDictionary<string, string>? otherData, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var memoryValue = 0;

            if (otherData == null)
                return memoryValue;

            foreach (var (roomDataKey, roomDataValue) in otherData)
            {
                var valueByteCount = encoding.GetByteCount(roomDataValue);
                var keyByteCount = encoding.GetByteCount(roomDataKey);
                memoryValue += valueByteCount + keyByteCount;
            }

            return memoryValue;
        }


    }
}
