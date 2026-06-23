namespace Elympics.Events
{
    internal readonly struct ElympicsLogEvent
    {
        public LogLevel LogLevel { get; init; }
        public string Time { get; init; }
        public string Json { get; init; }
    }
}
