using System;
using UnityEngine;

namespace Elympics
{
	public class ServerLogBehaviour : ElympicsMonoBehaviour, IInitializable
	{
		public bool disableInEditor = true;
		public Verbosity verbosity = Verbosity.Log;
		public string serverLogPrefix = "[Elympics::Server]";

		private readonly ElympicsArray<ElympicsLog> _logs       = new ElympicsArray<ElympicsLog>(5, () => new ElympicsLog());
		private          int                        _currentLog = 0;

		public static Verbosity LogTypeToVerbosity(LogType logType)
		{
			switch (logType)
			{
				case LogType.Assert:
				case LogType.Error:
				case LogType.Exception:
					return Verbosity.Error;
				case LogType.Warning:
					return Verbosity.Warning;
				case LogType.Log:
					return Verbosity.Log;
				default:
					return Verbosity.Log;
			}
		}

		public void Initialize()
		{
			if (Application.isEditor && disableInEditor)
				return;
			if (Elympics.IsServer)
				Application.logMessageReceived += HandleLogReceived;
			else
				foreach (var log in _logs.Values) log.ValueChanged += HandleLogValueChanged;
		}

		private void HandleLogReceived(string message, string stackTrace, LogType type)
		{
			if (message.StartsWith(serverLogPrefix))
				return;
			_logs.Values[_currentLog].Value = (type, message);
			_currentLog = (_currentLog + 1) % _logs.Values.Count;
		}

		private void HandleLogValueChanged((LogType, string) lastValue, (LogType logType, string logMessage) newValue)
		{
			var messageVerbosity = LogTypeToVerbosity(newValue.logType);
			if (verbosity > messageVerbosity)
				return;
			var message = $"{serverLogPrefix} {newValue.logMessage}";
			Debug.unityLogger.Log(newValue.logType, (object) message, gameObject);
		}
	}

	public enum Verbosity
	{
		Log = 0,
		Warning = 1,
		Error = 2,
		None = 3
	}
}
