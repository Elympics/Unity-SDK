#nullable enable

using System.IO;
using System.Text;
using Elympics.Core.Logger;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.ParrelSync
{
    /// <summary>
    /// Makes a ParrelSync clone a half-remote client by default: ParrelSync's default argument <c>client</c> becomes
    /// <c>--elympics-launch-mode=halfRemoteClient --half-remote-player-index=&lt;clone number&gt;</c>, so pressing Play in the gameplay scene
    /// joins the half-remote server as that player. An argument edited in the Clones Manager is left alone.
    /// </summary>
    [InitializeOnLoad]
    internal static class ElympicsCloneArguments
    {
        /// <summary>What ParrelSync writes when it creates a clone.</summary>
        internal const string ParrelSyncDefault = "client";

        static ElympicsCloneArguments()
        {
            if (!ElympicsClonesManager.IsClone())
                return;
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (TryRewriteDefault(projectPath, ElympicsClonesManager.GetCloneNumber()))
                ElympicsLogger.LogInfo($"This clone's ParrelSync arguments are now \"{DefaultArguments(ElympicsClonesManager.GetCloneNumber())}\": "
                    + "pressing Play in the gameplay scene joins the half-remote server. Edit them in the Clones Manager.");
        }

        internal static string DefaultArguments(int cloneNumber) =>
            $"--elympics-launch-mode={LaunchDataFromParameters.HalfRemoteClient} --half-remote-player-index={cloneNumber}";

        /// <summary>Rewrites the argument file of the clone at <paramref name="projectPath"/> if it still holds ParrelSync's default.</summary>
        /// <returns>True if the file was rewritten.</returns>
        internal static bool TryRewriteDefault(string projectPath, int cloneNumber)
        {
            var path = Path.Combine(projectPath, CloneArguments.FileName);
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8).Trim() != ParrelSyncDefault)
                return false;
            File.WriteAllText(path, DefaultArguments(cloneNumber), Encoding.UTF8);
            return true;
        }
    }
}
