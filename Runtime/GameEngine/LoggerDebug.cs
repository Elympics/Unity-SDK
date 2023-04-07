using System;
using MatchTcpClients;

namespace Elympics
{
	public class LoggerDebug : IGameServerClientLogger
	{
		public void Verbose(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogFormat(message, arguments);
		}

		public void Debug(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogFormat(message, arguments);
		}

		public void Info(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogFormat(message, arguments);
		}

		public void Warning(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogWarningFormat(message, arguments);
		}

		public void Warning(string message, Exception exception, params object[] arguments)
		{
			UnityEngine.Debug.LogWarningFormat("Exception: {0}, Message: {1}", exception, string.Format(message, arguments));
		}

		public void Error(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogErrorFormat(message, arguments);
		}

		public void Error(string message, Exception exception, params object[] arguments)
		{
			UnityEngine.Debug.LogErrorFormat("Exception: {0}, Message: {1}", exception, string.Format(message, arguments));
		}

		public void Fatal(string message, params object[] arguments)
		{
			UnityEngine.Debug.LogErrorFormat(message, arguments);
		}

		public void Fatal(string message, Exception exception, params object[] arguments)
		{
			UnityEngine.Debug.LogErrorFormat("Exception: {0}, Message: {1}", exception, string.Format(message, arguments));
		}
	}
}
