using System;
using UnityEditor;
using UnityEngine;

namespace Elympics.Weaver
{
    [Serializable]
    public struct ScriptingSymbols
    {
        [SerializeField]
        public string value;
        [SerializeField]
        private bool m_IsActive;

        /// <summary>
        /// Returns back true if the symbols are defined.
        /// </summary>
        public bool isActive => m_IsActive;

        public void ValidateSymbols()
        {
            if (string.IsNullOrEmpty(value))
            {
                m_IsActive = true;
                return;
            }

            var spitKey = new[] { ';' };

            var requiredDefines = value.Split(spitKey, StringSplitOptions.RemoveEmptyEntries);
            var activeDefines = EditorUserBuildSettings.activeScriptCompilationDefines;

            foreach (var user in requiredDefines)
            {
                var wasFound = false;
                var isInversed = user[0] == '!';
                var indexA = isInversed ? 1 : 0;

                foreach (var current in activeDefines)
                {

                    // Make sure we are the same length
                    if (user.Length - indexA != current.Length)
                    {
                        continue;
                    }

                    if (string.Compare(user, indexA, current, 0, current.Length) == 0)
                    {
                        wasFound = true;
                        break;
                    }
                }

                if (wasFound == isInversed)
                {
                    m_IsActive = false;
                    return;
                }
            }

            m_IsActive = true;
        }
    }
}
