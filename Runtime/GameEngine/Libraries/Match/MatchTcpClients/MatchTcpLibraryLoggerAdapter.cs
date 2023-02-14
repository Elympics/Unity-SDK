using System;
using MatchTcpLibrary;

namespace MatchTcpClients
{
	public class MatchTcpLibraryLoggerAdapter : IMatchTcpLibraryLogger
	{
		private readonly IGameServerClientLogger _gameServerClientLogger;

		public MatchTcpLibraryLoggerAdapter(IGameServerClientLogger gameServerClientLogger)
		{
			_gameServerClientLogger = gameServerClientLogger;
		}

		public void Verbose(string message, params object[] arguments)
		{
			_gameServerClientLogger.Verbose(message, arguments);
		}

		public void Debug(string message, params object[] arguments)
		{
			_gameServerClientLogger.Debug(message, arguments);
		}

		public void Info(string message, params object[] arguments)
		{
			_gameServerClientLogger.Info(message, arguments);
		}

		public void Warning(string message, params object[] arguments)
		{
			_gameServerClientLogger.Warning(message, arguments);
		}

		public void Warning(string message, Exception exception, params object[] arguments)
		{
			_gameServerClientLogger.Warning(message, exception, arguments);
		}

		public void Error(string message, params object[] arguments)
		{
			_gameServerClientLogger.Error(message, arguments);
		}

		public void Error(string message, Exception exception, params object[] arguments)
		{
			_gameServerClientLogger.Error(message, exception, arguments);
		}

		public void Fatal(string message, params object[] arguments)
		{
			_gameServerClientLogger.Fatal(message, arguments);
		}

		public void Fatal(string message, Exception exception, params object[] arguments)
		{
			_gameServerClientLogger.Fatal(message, exception, arguments);
		}
	}
}
