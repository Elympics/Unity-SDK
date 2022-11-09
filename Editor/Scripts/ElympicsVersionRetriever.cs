using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Elympics
{
	internal static class ElympicsVersionRetriever
	{

		private static ListRequest _searchIsCompleted;
		private static Action<string> _onCompletion;
		private static string _elympicsName = "Elympics";


		public static void GetVersion(Action<string> onCompletion)
		{
			if (_searchIsCompleted == null)
			{
				_searchIsCompleted = Client.List();
				_onCompletion = onCompletion;
				EditorApplication.update += Progress;
			}
		}

		static void Progress()
		{
			if (!_searchIsCompleted.IsCompleted)
			{
				return;
			}

			try
			{
				if (_searchIsCompleted.Status == StatusCode.Success)
				{
					var version = _searchIsCompleted.Result.FirstOrDefault(item => item.name == _elympicsName)?.version;
					if (!string.IsNullOrEmpty(version))
					{
						_onCompletion?.Invoke(version);
						return;
					}
				}

				var assemblies = AppDomain.CurrentDomain.GetAssemblies();

				foreach (var assembly in assemblies)
				{
					var asmname = assembly.GetName().Name;
					if (asmname.Equals(_elympicsName))
					{
						var version = assembly.GetName().Version;
						_onCompletion?.Invoke($"{version.Major}.{version.Minor}.{version.Build}");
						break;
					}
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
			finally
			{
				_onCompletion = null;
				_searchIsCompleted = null;
				EditorApplication.update -= Progress;
			}
		}
	}
}
