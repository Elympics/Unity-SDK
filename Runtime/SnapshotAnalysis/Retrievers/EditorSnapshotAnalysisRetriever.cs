#nullable enable

using System;
using System.IO;
using System.Linq;
using Elympics.SnapshotAnalysis.Serialization;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    /// <summary>Reads a replay from the local file system.</summary>
    internal class EditorSnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        public EditorSnapshotAnalysisRetriever(string path)
        {
            if (!TryResolvePath(path, out var resolvedPath, out var error))
                throw new ArgumentException(error, nameof(path));

            try
            {
                using var stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read);
                Replay = SnapshotDeserializer.DeserializeSnapshots(stream);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to deserialize snapshots from file \"{resolvedPath}\".", e);
            }
        }

        /// <summary>
        /// Resolves <paramref name="path"/> to an existing replay file. A path ending with a directory separator
        /// (e.g. "C:\Users\Elympics\Snapshots\") means the newest replay file in that directory, and the replay extension is optional.
        /// </summary>
        internal static bool TryResolvePath(string? path, out string resolvedPath, out string error)
        {
            resolvedPath = string.Empty;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Path to snapshot replay file can't be empty.";
                return false;
            }

            var extension = EditorSnapshotAnalysisCollector.DefaultFileExtension;
            if (path!.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                if (!Directory.Exists(path))
                {
                    error = $"Directory \"{path}\" does not exist or can't be accessed.";
                    return false;
                }
                var newest = Directory.EnumerateFiles(path, "*" + extension).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
                if (string.IsNullOrEmpty(newest))
                {
                    error = $"No files with extension \"{extension}\" found in directory \"{path}\".";
                    return false;
                }
                resolvedPath = newest;
                return true;
            }

            if (!path.EndsWith(extension))
                path += extension;
            if (!File.Exists(path))
            {
                error = $"File \"{path}\" does not exist or can't be accessed.";
                return false;
            }
            resolvedPath = path;
            return true;
        }
    }
}
