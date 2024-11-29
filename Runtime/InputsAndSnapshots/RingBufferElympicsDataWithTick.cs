using System;
using System.Collections.Generic;
using System.Threading;

namespace Elympics
{
    /// <summary>
    /// Adds tick based data. If data exceeds MaxTick, removes oldest record and add new one on the end.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RingBufferElympicsDataWithTick<T> : IDisposable
        where T : ElympicsDataWithTick
    {
        private readonly int _bufferSize;
        private readonly SortedList<long, T> _inputs;
        private readonly ReaderWriterLockSlim _lock = new();

        private long _minTick;

        // ReSharper disable once InconsistentNaming
        private long _maxTick => _minTick + _bufferSize - 1;

        public RingBufferElympicsDataWithTick(int bufferSize)
        {
            _bufferSize = bufferSize;
            _inputs = new SortedList<long, T>(bufferSize);
            _minTick = 0;
        }

        public long MinTick()
        {

            _lock.EnterReadLock();
            try
            {
                return _minTick;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public long MaxTick()
        {
            _lock.EnterReadLock();

            try
            {
                return _maxTick;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool TryAddData(T data)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!IsTickInMinTickRange(data.Tick))
                    return false;
                if (_inputs.ContainsKey(data.Tick))
                    return true;
                _lock.EnterWriteLock();
                try
                {
                    if (!IsTickInMaxTickRange(data.Tick))
                    {
                        var newMinTick = data.Tick - (_bufferSize - 1);
                        if (TryUpdateMinTick(newMinTick, out var lastMinTick))
                            UpdateBufferForNewMinTick(newMinTick, lastMinTick);
                    }
                    _inputs[data.Tick] = data;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                return true;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public int Count()
        {
            _lock.EnterReadLock();
            try
            {
                return _inputs.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool TryAddOrReplaceData(T data)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!IsTickInMaxTickRange(data.Tick))
                    return false;
                _lock.EnterWriteLock();
                try
                {
                    _inputs[data.Tick] = data;
                    return true;

                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public bool TryGetDataForTick(long tick, out T data)
        {

            _lock.EnterReadLock();
            try
            {
                return _inputs.TryGetValue(tick, out data);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void UpdateMinTick(long minTick)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!TryUpdateMinTick(minTick, out var lastMinTick))
                    return;

                UpdateBufferForNewMinTick(minTick, lastMinTick);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        private void UpdateBufferForNewMinTick(long newMinTick, long lastMinTick)
        {

            if (newMinTick > lastMinTick)
                RemoveFirstTicksToCurrentMinTick();
            if (newMinTick < lastMinTick)
                RemoveLastTicksToCurrentMaxTick();
        }

        private bool TryUpdateMinTick(long minTick, out long lastMinTick)
        {
            lastMinTick = 0;
            if (minTick < _minTick)
                return false;

            lastMinTick = _minTick;
            _minTick = minTick;
            return true;
        }

        public void GetInputListNonAlloc(T[] copyBuffer, out int size)
        {
            if (copyBuffer.Length < _bufferSize)
                throw new ArgumentException($"Copy buffer size: {copyBuffer.Length} cannot be less than collection size: {_bufferSize}");

            _lock.EnterReadLock();
            size = _inputs.Count;
            try
            {
                _inputs.Values.CopyTo(copyBuffer, 0);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private bool IsTickInMinTickRange(long tick) => tick >= _minTick;
        private bool IsTickInMaxTickRange(long tick) => tick <= _maxTick;

        private void RemoveLastTicksToCurrentMaxTick()
        {
            for (var i = _inputs.Count - 1; i >= 0; i--)
            {
                if (_inputs.Keys[i] <= _maxTick)
                    break;
                _inputs.RemoveAt(i);
            }
        }

        private void RemoveFirstTicksToCurrentMinTick()
        {
            while (_inputs.Count > 0 && _inputs.Keys[0] < _minTick)
                _inputs.RemoveAt(0);
        }
        public void Dispose() => _lock?.Dispose();
    }
}
