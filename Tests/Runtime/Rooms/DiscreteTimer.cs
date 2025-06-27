using System;

namespace Elympics.Tests.Rooms
{
    public class DiscreteTimer
    {
        public TimeSpan Interval { private get; init; } = TimeSpan.FromSeconds(1);
        public DateTime Time { get; private set; } = DateTime.UtcNow;

        public DiscreteTimer()
        { }

        public DiscreteTimer(DiscreteTimer other) => (Interval, Time) = (other.Interval, other.Time);

        public static DiscreteTimer operator ++(DiscreteTimer timer) => new(timer)
        {
            Time = timer.Time + timer.Interval,
        };

        public static implicit operator DateTime(DiscreteTimer timer) => timer.Time;
    }
}
