#nullable enable
using System;

namespace Elympics.Core.Logger.Builder
{
    internal interface ILogOutlet
    {
        void Log(
            LogCategory category,
            DateTime time,
            string message,
            string? stacktrace,
            ApplicationState state,
            LoggerConfig config);
    }
}
