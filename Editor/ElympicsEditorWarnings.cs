using System.Linq;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public static class ElympicsEditorWarnings
    {
        [DidReloadScripts]
        private static void CheckElympicsVars()
        {
            var baseSynchronizationClassesInApplication = SceneObjectsFinder.FindObjectsOfType<IObservable>(SceneManager.GetActiveScene(), true)
                .Select(obj => obj.GetType());
            foreach (var customClass in baseSynchronizationClassesInApplication.Distinct())
            {
                var type = customClass;

                while (typeof(IObservable).IsAssignableFrom(type.BaseType))
                {
                    type = type.BaseType;

                    var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var field in fields)
                    {
                        var isElympicsVar = typeof(ElympicsVar).IsAssignableFrom(field.FieldType);

                        if (field.IsPrivate && isElympicsVar)
                            Debug.LogWarning($"WARNING! Private ElympicsVar ({field}) in base {type} of class {customClass} isn't synchronized! Try making it protected instead.");
                    }
                }
            }
        }
    }
}
