using System;
using GameBotCore.V1._1;
using GameEngineCore.V1._1;

namespace Elympics
{
    public class LoggerNoop : IGameEngineLogger, IGameBotLogger
    {
        public void Verbose(string message, params object[] arguments)
        {
        }

        public void Debug(string message, params object[] arguments)
        {
        }

        public void Info(string message, params object[] arguments)
        {
        }

        public void Warning(string message, params object[] arguments)
        {
        }

        public void Warning(string message, Exception exception, params object[] arguments)
        {
        }

        public void Error(string message, params object[] arguments)
        {
        }

        public void Error(string message, Exception exception, params object[] arguments)
        {
        }

        public void Fatal(string message, params object[] arguments)
        {
        }

        public void Fatal(string message, Exception exception, params object[] arguments)
        {
        }
    }
}
