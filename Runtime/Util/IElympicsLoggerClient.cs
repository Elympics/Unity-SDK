using Elympics.ElympicsSystems.Internal;
namespace Elympics
{
    internal interface IElympicsLoggerClient
    {
        void LogCaptured(ElympicsLoggerContext context, LogLevel level);
    }
}
