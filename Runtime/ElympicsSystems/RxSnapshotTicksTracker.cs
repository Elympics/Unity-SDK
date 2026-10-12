using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.AssemblyCommunicator.Events;

namespace Elympics.ElympicsSystems
{
    internal class RxSnapshotTicksTracker
    {
        private readonly double _windowSeconds;
        private int _windowTicks;
        private readonly HashSet<long> _ticks;

        public ReceivedStatsUpdated CurrentState => new()
        {
            received = _ticks.Count,
            total = _ticks.Count > 0 ? _ticks.Max() - _ticks.Min() + 1 : 0,
        };

        public RxSnapshotTicksTracker(TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(window));
            _windowSeconds = window.TotalSeconds;
            _windowTicks = (int)Math.Ceiling(_windowSeconds);
            _ticks = new HashSet<long>();
        }

        public void Initialize(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            _windowTicks = (int)(ticksPerSecond * _windowSeconds);
            _ticks.Clear();
            _ = _ticks.EnsureCapacity(_windowTicks);
        }

        public void Update(long tick)
        {
            if (tick <= 0)
                throw new ArgumentOutOfRangeException(nameof(tick));
            _ = _ticks.Add(tick);
            _ = _ticks.RemoveWhere(t => tick - t >= _windowTicks);
        }

        public void Clear() => _ticks.Clear();
    }
}
