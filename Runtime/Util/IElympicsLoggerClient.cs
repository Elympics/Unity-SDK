using Elympics.ElympicsSystems.Internal;
namespace Elympics
{
    internal interface IElympicsLoggerClient
    {
        void LogCaptured(string message, string time, ElympicsLoggerContext log, LogLevel level);
    }
}
