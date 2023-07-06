using System;

namespace Elympics
{
    public class LeaderboardTimeScope
    {
        public LeaderboardTimeScopeType LeaderboardTimeScopeType { get; }
        public DateTimeOffset DateFrom { get; }
        public DateTimeOffset DateTo { get; }

        private const int MaxDaysSpan = 31;

        public LeaderboardTimeScope(LeaderboardTimeScopeType leaderboardTimeScopeType)
        {
            if (!Enum.IsDefined(typeof(LeaderboardTimeScopeType), leaderboardTimeScopeType))
                throw new ArgumentOutOfRangeException(nameof(leaderboardTimeScopeType));

            if (leaderboardTimeScopeType == LeaderboardTimeScopeType.Custom)
                throw new ArgumentException("To declare custom time scope use constructor with DateTime parameters");

            LeaderboardTimeScopeType = leaderboardTimeScopeType;
        }

        public LeaderboardTimeScope(DateTimeOffset dateFrom, DateTimeOffset dateTo)
        {
            if (dateFrom >= dateTo)
                throw new ArgumentException($"{nameof(dateFrom)} must be before {nameof(dateTo)}");

            if ((dateTo - dateFrom).TotalDays > MaxDaysSpan)
                throw new ArgumentException($"Maximal accepted date difference is {MaxDaysSpan} days");

            LeaderboardTimeScopeType = LeaderboardTimeScopeType.Custom;
            DateFrom = dateFrom;
            DateTo = dateTo;
        }

        public LeaderboardTimeScope(DateTimeOffset dateStart, TimeSpan timeSpan) : this(dateStart, dateStart + timeSpan)
        { }
    }

    public enum LeaderboardTimeScopeType
    {
        AllTime = 0,
        Month = 1,
        Day = 2,
        Custom = 3,
    }
}
