using System.Collections.Generic;

namespace Elympics
{
    public class ElympicsDataWithTickBuffer<T>
        where T : ElympicsDataWithTick
    {
        private readonly int _bufferSize;
        private readonly SortedList<long, T> _inputs;

        public long MinTick { get; private set; }
        public long MaxTick => MinTick + _bufferSize - 1;

        public ElympicsDataWithTickBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _inputs = new SortedList<long, T>(bufferSize);

            MinTick = 0;
        }

        public bool TryAddData(T data)
        {
            lock (_inputs)
            {
                if (!IsTickInRange(data.Tick))
                    return false;
                if (_inputs.ContainsKey(data.Tick))
                    return true;
                _inputs[data.Tick] = data;
                return true;
            }
        }

        public bool TryAddOrReplaceData(T data)
        {
            lock (_inputs)
            {
                if (!IsTickInRange(data.Tick))
                    return false;
                _inputs[data.Tick] = data;
                return true;
            }
        }

        private bool IsTickInRange(long tick) => tick >= MinTick && tick <= MaxTick;

        public bool TryGetDataForTick(long tick, out T data)
        {
            lock (_inputs)
                return _inputs.TryGetValue(tick, out data);
        }

        public void UpdateMinTick(long minTick)
        {
            lock (_inputs)
            {
                if (minTick == MinTick)
                    return;

                var lastMinTick = MinTick;
                MinTick = minTick;

                if (minTick > lastMinTick)
                    RemoveFirstTicksToCurrentMinTick();
                if (minTick < lastMinTick)
                    RemoveLastTicksToCurrentMaxTick();
            }
        }

        private void RemoveLastTicksToCurrentMaxTick()
        {
            lock (_inputs)
                for (var i = _inputs.Count - 1; i >= 0; i--)
                {
                    if (_inputs.Keys[i] <= MaxTick)
                        break;
                    _inputs.RemoveAt(i);
                }
        }

        private void RemoveFirstTicksToCurrentMinTick()
        {
            lock (_inputs)
                while (_inputs.Count > 0 && _inputs.Keys[0] < MinTick)
                    _inputs.RemoveAt(0);
        }
    }
}
