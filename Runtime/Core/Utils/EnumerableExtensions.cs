#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elympics.Core.Utils
{
    public static class EnumerableExtensions
    {
        /// <param name="lineCount">Number of new lines to insert after each item.</param>
        public static string NewLineList<T>(this IEnumerable<T?> enumerable, int lineCount = 1)
        {
            var sb = new StringBuilder();

            foreach (var item in enumerable)
            {
                _ = sb.Append(item?.ToString() ?? "null");

                for (var i = 0; i < lineCount; i++)
                {
                    _ = sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static string CommaList<T>(this IEnumerable<T> enumerable)
        {
            var sb = new StringBuilder();
            using var enumerator = enumerable.GetEnumerator();

            if (enumerator.MoveNext())
                _ = sb.Append(enumerator.Current);

            while (enumerator.MoveNext())
                _ = sb.Append(", ").Append(enumerator.Current);

            return sb.ToString();
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : class => enumerable.Where(x => x != null)!;
    }
}
