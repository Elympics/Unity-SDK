using Elympics.ElympicsSystems.Internal;

namespace Elympics.Events
{
    internal readonly struct ElympicsLogEvent
    {
        public string Message { get; init; }
        public string Time { get; init; }
        public ElympicsLoggerContext Context { get; init; }
        public LogLevel LogLevel { get; init; }
    }
}
