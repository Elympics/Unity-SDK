#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private int _end;

        public RingBuffer(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("Circular buffer cannot have negative or zero capacity.", nameof(capacity));

            _buffer = new T[capacity];
            _end = Count == capacity ? 0 : Count;
        }

        public T this[int i] => i < Count ? _buffer[i] : throw new IndexOutOfRangeException();

        public int Count { get; private set; }

        private bool IsFull => Count == _buffer.Length;
        private bool IsEmpty => Count == 0;

        public void PushBack(T item)
        {
            if (IsFull)
            {
                _buffer[_end] = item;
                Increment(ref _end);
            }
            else
            {
                _buffer[_end] = item;
                Increment(ref _end);
                ++Count;
            }
        }

        public List<T> ToList() => _buffer.Take(Count).ToList();

        public T[] ToArray()
        {
            var newArray = new T[Count];
            var newArrayOffset = 0;

            if (IsEmpty)
                return newArray;

            Array.Copy(_buffer, 0, newArray, newArrayOffset, Count);
            return newArray;
        }

        private void Increment(ref int index)
        {
            if (++index == _buffer.Length)
                index = 0;
        }
    }
}
