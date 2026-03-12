using System;
using System.IO;
using UnityEngine;

namespace Elympics.Weaver
{
    /// <summary>
    /// Keeps track of the assembly path and if the
    /// weaving is enabled or not.
    /// </summary>
    [Serializable]
    public class WeavedAssembly
    {
        public delegate void WeavedAssemblyDelegate(WeavedAssembly weavedAssembly);

        [SerializeField]
        private string m_RelativePath = "";
        [SerializeField]
        private bool m_IsActive = true;
        [SerializeField]
        private bool shouldThrowIfNotFound = true;

        /// <summary>
        /// Returns back the file path to this assembly
        /// </summary>
        public string RelativePath
        {
            get => m_RelativePath;
            set => m_RelativePath = value;
        }

        /// <summary>
        /// Returns true if this assembly should be modified
        /// by Weaver or not.
        /// </summary>
        public bool IsActive
        {
            get => m_IsActive;
            set => m_IsActive = value;
        }

        public bool ShouldThrowIfNotFound
        {
            get => shouldThrowIfNotFound;
            set => shouldThrowIfNotFound = value;
        }

        /// <summary>
        /// Returns back if this module currently has debug symbols.
        /// </summary>
        public bool HasMonoDebugSymbols => File.Exists(GetSystemPath() + ".mdb");

        /// <summary>
        /// Returns back if this assembly exists on disk.
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            return File.Exists(GetSystemPath());
        }

        /// <summary>
        /// Returns back the system path to this
        /// assembly.
        /// </summary>
        public string GetSystemPath() => GetSystemPath(RelativePath);

        public static string GetSystemPath(string relativePath)
        {
            // Get our path
            var path = Application.dataPath;
            // Get the length
            var pathLength = path.Length;
            // Split it
            path = path[..(pathLength - /* Assets */ 6)];
            // Add our relative path
            path = Path.Combine(path, relativePath);
            // Return the result
            return path;
        }

        public override string ToString() =>
            $"[{(IsActive ? "a" : "-")}{(ShouldThrowIfNotFound ? "t" : "-")}] {GetSystemPath()}";
    }
}
