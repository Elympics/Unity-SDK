#nullable enable

using System.IO;
using System.Text;
using Elympics.Core.Logger;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.ParrelSync
{
    [InitializeOnLoad]
    internal static class ElympicsCloneArguments
    {
        internal const string ParrelSyncDefault = "client";

        static ElympicsCloneArguments()
        {
            if (!ElympicsClonesManager.IsClone())
                return;
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (TryRewriteDefault(projectPath, ElympicsClonesManager.GetCloneNumber()))
                ElympicsLogger.LogInfo($"This clone's ParrelSync arguments are now \"{GetDefaultArguments(ElympicsClonesManager.GetCloneNumber())}\": "
                    + "pressing Play in the gameplay scene joins the half-remote server. Edit them in the Clones Manager.");
        }

        internal static string GetDefaultArguments(int cloneNumber)
        {
            var parameters = ApplicationParameters.Parameters;
            return $"--{parameters.LaunchMode.LongArgumentName}={LaunchDataFromParameters.HalfRemoteClient} --{parameters.HalfRemotePlayerIndex.LongArgumentName}={cloneNumber}";
        }

        internal static bool TryRewriteDefault(string projectPath, int cloneNumber)
        {
            var path = Path.Combine(projectPath, CloneArguments.FileName);
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8).Trim() != ParrelSyncDefault)
                return false;
            File.WriteAllText(path, GetDefaultArguments(cloneNumber), Encoding.UTF8);
            return true;
        }
    }
}
