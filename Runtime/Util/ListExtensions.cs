#nullable enable

using System.Collections.Generic;

namespace Assets.Plugins.Elympics.Runtime.Util
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
        /// If the current capacity is less than <paramref name="capacity"/>, it is increased to at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <remarks>This method provides same functionality as method added to <see cref="List{T}"/> class in .NET 6.</remarks>
        internal static void EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }
    }
}
