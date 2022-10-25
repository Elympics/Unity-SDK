using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Reflection;

namespace Elympics
{
	public static class ElympicsEditorWarnings
	{
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void CheckElympicsVars()
		{
			var baseSynchronizationClassesInApplication = SceneObjectsFinder.FindObjectsOfType<IObservable>(SceneManager.GetActiveScene(), true);
			foreach (var customClass in baseSynchronizationClassesInApplication)
			{
				var type = customClass.GetType();

				while (typeof(IObservable).IsAssignableFrom(type.BaseType))
				{
					type = type.BaseType;

					FieldInfo[] fields = type
							.GetFields(
							 BindingFlags.NonPublic |
							 BindingFlags.Instance);

					foreach (var field in fields)
					{
						var isElympicsVar = typeof(ElympicsVar).IsAssignableFrom(field.FieldType);

						if (field.IsPrivate && isElympicsVar)
							Debug.LogWarning($"WARNING! Private ElympicsVars ({field}) in base {customClass} class aren't synchronized! Try making them protected instead.");
					}
				}
			}
		}
	}
}