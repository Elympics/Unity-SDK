using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Elympics.Weaver
{
    public static class AssemblyUtility
    {
        private static IList<AssemblyDescription> assemblies;

        /// <summary>
        /// Returns the cached array of user assemblies. If you wan to refresh
        /// call <see cref="PopulateAssemblyCache"/>
        /// </summary>
        /// <returns></returns>
        public static IList<AssemblyDescription> GetUserCachedAssemblies()
        {
            return assemblies;
        }

        /// <summary>
        /// Populates our list of loaded assemblies
        /// </summary>
        public static void PopulateAssemblyCache()
        {
            var assemblyPaths = GetUserAssemblyPaths();
            assemblies = new AssemblyDescription[assemblyPaths.Count];
            for (var i = 0; i < assemblyPaths.Count; i++)
            {
                assemblies[i] = new AssemblyDescription(assemblyPaths[i]);
            }
        }

        /// <summary>
        /// Forces Unity to recompile all scripts and then refresh.
        /// </summary>
        ///
        public static void DirtyAllScripts()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            // Force the database to refresh.
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Returns back true if the dll at the path is a managed dll.
        /// </summary>
        /// <param name="systemPath">The full system path to the dll.</param>
        /// <returns>True if a managed dll and false if not. </returns>
        public static bool IsManagedAssembly(string systemPath)
        {
            var dllType = InternalEditorUtility.DetectDotNetDll(systemPath);
            return dllType is not DllType.Unknown and not DllType.Native;
        }

        /// <summary>
        /// Returns back all the user assemblies define in the unity project.
        /// </summary>
        /// <returns></returns>
        public static IList<string> GetUserAssemblyPaths()
        {
            var assemblies = new List<string>(20);
            FindAssemblies(Application.dataPath, 120, assemblies);
            FindAssemblies(Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies"), 2, assemblies);
            return assemblies;
        }

        /// <summary>
        /// Gets a list of all the user Assemblies and returns their
        /// project path.
        /// </summary>
        /// <returns></returns>
        public static IList<string> GetRelativeUserAssemblyPaths()
        {
            var assemblies = GetUserAssemblyPaths();
            // Loop over them all
            for (var i = 0; i < assemblies.Count; i++)
            {
                assemblies[i] = FileUtility.SystemToProjectPath(assemblies[i]);
            }
            return assemblies;
        }

        /// <summary>
        /// Finds all the managed assemblies at the give path. It will look into sub folders
        /// up until the max depth.
        /// </summary>
        /// <param name="systemPath">The path of the directory you want to start looking in.</param>
        /// <param name="maxDepth">The max number of sub directories you want to go into.</param>
        /// <returns></returns>
        public static void FindAssemblies(string systemPath, int maxDepth, List<string> result)
        {
            if (maxDepth > 0)
            {
                try
                {
                    if (Directory.Exists(systemPath))
                    {
                        var directroyInfo = new DirectoryInfo(systemPath);
                        // Find all assemblies that are managed
                        var pathsToAdd = from file in directroyInfo.GetFiles()
                                         where IsManagedAssembly(file.FullName)
                                         select FileUtility.Normalize(file.FullName);
                        result.AddRange(pathsToAdd);
                        var directories = directroyInfo.GetDirectories();
                        for (var i = 0; i < directories.Length; i++)
                        {
                            var current = directories[i];
                            FindAssemblies(current.FullName, maxDepth - 1, result);
                        }
                    }
                }
                catch
                {
                    // Nothing to do here
                }
            }
        }
    }
}
