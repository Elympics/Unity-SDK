using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameEngineCore.V1._1;
using UnityEngine;

namespace Elympics
{
	public class LogHandler
	{
		private const int MaxStackFramesToLog = 5;
		private const int LogsSkipCallbackFrames = 3;
		private const string LogsAtUnityEngineNamespace = "  at " + nameof(UnityEngine);

		private readonly StringBuilder _loggerSb = new StringBuilder();

		public IGameEngineLogger Logger { private get; set; }

		public LogHandler()
		{
			Application.logMessageReceived += OnLogMessageReceived;
		}

		private void OnLogMessageReceived(string condition, string trace, LogType type)
		{
			if (type != LogType.Log && trace == null)
				trace = GetStackTraceForLog();

			switch (type)
			{
				case LogType.Error:
					LogToStderr(condition, trace);
					Logger?.Error("{0}\n{1}", condition, trace);
					break;
				case LogType.Assert:
					LogToStderr(condition, trace);
					Logger?.Fatal("{0}\n{1}", condition, trace);
					break;
				case LogType.Warning:
					Logger?.Warning("{0}\n{1}", condition, trace);
					break;
				case LogType.Log:
					Logger?.Info("{0}", condition);
					break;
				case LogType.Exception:
					LogToStderr(condition, trace);
					Logger?.Error("{0}\n{1}", condition, trace);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		private static void LogToStderr(string message, string trace)
		{
			Console.Error.WriteLine($"{message}\n{trace}\n\n");
		}

		private string GetStackTraceForLog()
		{
			var stackTrace = SplitToLines(Environment.StackTrace);
			stackTrace = stackTrace.Skip(LogsSkipCallbackFrames).SkipWhile(x => x.StartsWith(LogsAtUnityEngineNamespace)).Take(MaxStackFramesToLog);
			_loggerSb.Clear();
			foreach (var frame in stackTrace)
				_loggerSb.AppendLine(frame);

			return _loggerSb.ToString();
		}

		private static IEnumerable<string> SplitToLines(string input)
		{
			using (var reader = new StringReader(input))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
					yield return line;
			}
		}
	}
}
