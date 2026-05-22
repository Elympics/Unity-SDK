using Elympics.Core.Logger;

namespace Elympics.Events
{
    internal readonly struct ElympicsLogEvent
    {
        public string Message { get; init; }
        public string Time { get; init; }
        public ApplicationState Context { get; init; }
        public LogLevel LogLevel { get; init; }
    }
}
