using System;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Elympics.Replication
{
    /// <summary>
    /// A lightweight readonly ref struct view over a jagged <typeparamref name="T"/>[][] array
    /// paired with a per-row count array. Because it is a ref struct it lives only on the stack —
    /// owning classes keep the raw <c>T[][]</c> and <c>int[]</c> arrays and construct this view
    /// when calling pipeline systems.
    /// <para>
    /// The first dimension is the row index (typically player index); the second dimension is the
    /// column index (entity slot). Only columns <c>[0, RowCount(row))</c> are valid per row.
    /// </para>
    /// <para>
    /// Column capacity is always >= world.DenseCapacity >= world.DenseCount. Since pipeline systems
    /// only produce subsets of dense indices, Append can never exceed capacity.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The element type. Must be a value type for AOT/IL2CPP compatibility.</typeparam>
    internal readonly ref struct PackedArray2D<T> where T : struct
    {
        private readonly T[][] _items;
        private readonly int[] _rowCounts;

        /// <summary>Number of rows (first dimension) in the backing array.</summary>
        internal int RowCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items.Length;
        }

        /// <summary>Number of columns (second dimension) in the backing array (row 0 length).</summary>
        internal int ColumnCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items[0].Length;
        }

        /// <summary>
        /// Gets or sets the element at <c>[row, column]</c>.
        /// Column must be in <c>[0, RowCount(row))</c>; checked via Debug.Assert in development builds.
        /// </summary>
        internal T this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D row {row} out of range [0, {RowCapacity}).");
                Debug.Assert((uint)column < (uint)_rowCounts[row], $"PackedArray2D column {column} out of range [0, {_rowCounts[row]}) for row {row}.");
                return _items[row][column];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D row {row} out of range [0, {RowCapacity}).");
                Debug.Assert((uint)column < (uint)_rowCounts[row], $"PackedArray2D column {column} out of range [0, {_rowCounts[row]}) for row {row}.");
                _items[row][column] = value;
            }
        }

        /// <summary>Returns the number of valid elements in the specified <paramref name="row"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int RowCount(int row)
        {
            Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D.RowCount: row {row} out of range [0, {RowCapacity}).");
            return _rowCounts[row];
        }

        /// <summary>
        /// Creates a view over existing backing arrays. Both arrays must be non-null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PackedArray2D(T[][] backingArray, int[] rowCounts)
        {
            Debug.Assert(backingArray != null, "PackedArray2D backing array must not be null.");
            Debug.Assert(rowCounts != null, "PackedArray2D row counts must not be null.");
            Debug.Assert(backingArray.Length == rowCounts.Length,
                $"PackedArray2D row dimension mismatch: array has {backingArray.Length} rows but rowCounts has {rowCounts.Length} entries.");
            _items = backingArray;
            _rowCounts = rowCounts;
        }

        /// <summary>
        /// Appends <paramref name="item"/> to the end of <paramref name="row"/> and increments
        /// that row's count. Asserts that column capacity is not exceeded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Append(int row, T item)
        {
            Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D.Append: row {row} out of range [0, {RowCapacity}).");
            var count = _rowCounts[row];
            Debug.Assert(count < _items[row].Length, $"PackedArray2D.Append: column capacity {_items[row].Length} exceeded for row {row}.");
            _items[row][count] = item;
            _rowCounts[row] = count + 1;
        }

        /// <summary>
        /// Swap-removes the element at <paramref name="column"/> in <paramref name="row"/> by moving
        /// the last element into the vacated slot and decrementing that row's count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(int row, int column)
        {
            Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D.Remove: row {row} out of range [0, {RowCapacity}).");
            Debug.Assert((uint)column < (uint)_rowCounts[row], $"PackedArray2D.Remove: column {column} out of range [0, {_rowCounts[row]}) for row {row}.");
            _rowCounts[row]--;
            _items[row][column] = _items[row][_rowCounts[row]];
        }

        /// <summary>Returns a <see cref="ReadOnlySpan{T}"/> over the valid elements in the specified <paramref name="row"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<T> RowSpan(int row)
        {
            Debug.Assert((uint)row < (uint)RowCapacity, $"PackedArray2D.RowSpan: row {row} out of range [0, {RowCapacity}).");
            return _items[row].AsSpan(0, _rowCounts[row]);
        }
    }
}
