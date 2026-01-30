using System;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Elympics.Replication
{
    /// <summary>
    /// A lightweight readonly ref struct view over a pre-allocated <typeparamref name="T"/> array
    /// with an explicit element count. Because it is a ref struct it lives only on the stack and
    /// cannot be stored as a field — owning classes keep the raw <c>T[]</c> and <c>int count</c>
    /// and construct this view when calling pipeline systems.
    /// </summary>
    /// <typeparam name="T">The element type. Must be a value type for AOT/IL2CPP compatibility.</typeparam>
    internal readonly ref struct PackedArray<T> where T : struct
    {
        private readonly Span<T> _span;

        /// <summary>Number of valid elements. Always in <c>[0, Capacity]</c>.</summary>
        internal int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        /// <summary>Total allocated length of the backing span.</summary>
        internal int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// Bounds checked via Debug.Assert in development builds only.
        /// </summary>
        internal T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert((uint)index < (uint)Count, $"PackedArray index {index} out of range [0, {Count}).");
                return _span[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Debug.Assert((uint)index < (uint)Count, $"PackedArray index {index} out of range [0, {Count}).");
                _span[index] = value;
            }
        }

        /// <summary>
        /// Creates a view over a backing array with the specified valid element count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PackedArray(T[] backingArray, int count)
        {
            Debug.Assert(backingArray != null, "PackedArray backing array must not be null.");
            Debug.Assert((uint)count <= (uint)backingArray.Length, "Initial count exceeds backing array length.");
            _span = backingArray.AsSpan();
            Count = count;
        }

        /// <summary>Returns a span over the valid elements <c>[0, Count)</c>.</summary>
        internal ReadOnlySpan<T> AsReadOnlySpan() => _span[..Count];
    }
}
