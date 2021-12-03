using System;

namespace Elympics
{
	public class RingBuffer<T>
	{
		private readonly T[] _buffer;
		private int _end;
		private int _size;

		public RingBuffer(int capacity)
		{
			if (capacity < 1)
				throw new ArgumentException("Circular buffer cannot have negative or zero capacity.", nameof(capacity));

			_buffer = new T[capacity];
			_end = _size == capacity ? 0 : _size;
		}

		private bool IsFull => _size == _buffer.Length;
		private bool IsEmpty => _size == 0;

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
				++_size;
			}
		}

		public T[] ToArray()
		{
			T[] newArray = new T[_size];
			int newArrayOffset = 0;

			if (IsEmpty)
				return newArray;

			Array.Copy(_buffer, 0, newArray, newArrayOffset, _size);
			return newArray;
		}

		private void Increment(ref int index)
		{
			if (++index == _buffer.Length)
				index = 0;
		}
	}
}