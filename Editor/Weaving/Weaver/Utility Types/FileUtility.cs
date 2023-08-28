using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elympics.Weaver
{
    [InitializeOnLoad]
    public static class FileUtility
    {
        private static readonly int ProjectPathLength;

        static FileUtility()
        {
            // Get our data path
            projectPath = Application.dataPath;
            // Remove 'Assets'
            projectPath = projectPath[..^6];
            // Store our length
            ProjectPathLength = projectPath.Length;
        }

        /// <summary>
        /// Gets the folder at the root of the project below 'Assets'
        /// </summary>
        public static string projectPath { get; private set; }

        /// <summary>
        /// Converts a full System Path to a Unity project relative path.
        /// </summary>
        public static string SystemToProjectPath(string systemPath)
        {
            var systemPathLength = systemPath.Length;
            var assetPathLength = systemPathLength - ProjectPathLength;
            if (assetPathLength <= 0)
            {
                throw new System.InvalidOperationException("Unable to convert system path to asset path");
            }
            return systemPath.Substring(ProjectPathLength, assetPathLength);
        }

        public static string Normalize(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
