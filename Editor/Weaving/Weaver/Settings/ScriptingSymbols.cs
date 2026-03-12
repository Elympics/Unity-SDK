using System;
using System.Linq;
using UnityEditor;

namespace Elympics.Weaver
{
    public static class ScriptingSymbols
    {
        public static bool ValidateSymbols(this string value)
        {
            value = value?.Trim();

            if (string.IsNullOrEmpty(value))
                return true;

            var splitKey = new[] { ';' };

            var requiredDefines = value.Split(splitKey, StringSplitOptions.RemoveEmptyEntries);
            var activeDefines = EditorUserBuildSettings.activeScriptCompilationDefines;

            foreach (var untrimmedDefine in requiredDefines)
            {
                var define = untrimmedDefine.Trim();
                var isInversed = define[0] == '!';
                var indexA = isInversed ? 1 : 0;

                var wasFound = activeDefines
                    .Where(current => define.Length - indexA == current.Length)
                    .Any(current => string.Compare(define, indexA, current, 0, current.Length) == 0);

                if (wasFound == isInversed)
                    return false;
            }

            return true;
        }
    }
}
