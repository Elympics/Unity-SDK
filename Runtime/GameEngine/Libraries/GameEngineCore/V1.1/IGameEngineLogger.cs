#nullable enable
using System;

namespace GameEngineCore.V1._1
{
    public interface IGameEngineLogger
    {
        void Verbose(string message, params object[] arguments);
        void Debug(string message, params object[] arguments);
        void Info(string message, params object[] arguments);
        void Warning(string message, params object[] arguments);
        void Warning(string message, Exception exception, params object[] arguments);
        void Error(string message, params object[] arguments);
        void Error(string message, Exception exception, params object[] arguments);
        void Fatal(string message, params object[] arguments);
        void Fatal(string message, Exception exception, params object[] arguments);
    }
}
