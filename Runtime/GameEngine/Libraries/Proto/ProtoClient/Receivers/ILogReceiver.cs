using ProtoLog;

namespace Proto.ProtoClient.Receivers
{
	internal interface ILogReceiver
	{
		void LogVerbose(LogVerboseMsg message);
		void LogDebug(LogDebugMsg message);
		void LogInfo(LogInfoMsg message);
		void LogWarning(LogWarningMsg message);
		void LogError(LogErrorMsg message);
		void LogFatal(LogFatalMsg msg);
	}
}
