#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elympics.SnapshotAnalysis.Serialization;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal class EditorSnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        public EditorSnapshotAnalysisRetriever(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path to snapshot replay file can't be empty.", nameof(path));

            //If path ends with directory separator (ex. "C:\Users\Elympics\Snapshots\"), look for the newest file with matching extension
            if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                var filePath = Directory.EnumerateFiles(path, "*" + EditorSnapshotAnalysisCollector.DefaultFileExtension).OrderByDescending(filePath => File.GetLastWriteTime(filePath))
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException($"No files with extension \"{EditorSnapshotAnalysisCollector.DefaultFileExtension}\" found in directory \"{path}\".", nameof(path));

                path = filePath;
            }
            else
            {
                if (!path.EndsWith(EditorSnapshotAnalysisCollector.DefaultFileExtension))
                    path += EditorSnapshotAnalysisCollector.DefaultFileExtension;

                if (!File.Exists(path))
                    throw new ArgumentException($"File \"{path}\" does not exist or can't be accessed.", nameof(path));
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                Replay = SnapshotDeserializer.DeserializeSnapshots(stream);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to deserialize snapshots from file \"{path}\".", e);
            }
        }
        public override SnapshotSaverInitData RetrieveInitData() => Replay.InitData;

        public override Dictionary<long, ElympicsSnapshotWithMetadata> RetrieveSnapshots() => Replay.Snapshots;
        public void AddStateMetaData(ElympicsBehavioursManager behaviourManager)
        {
            long? firstTick = null;
            foreach (var snapshotWithMeta in Replay.Snapshots)
            {
                firstTick ??= snapshotWithMeta.Key;
                behaviourManager.ApplySnapshot(snapshotWithMeta.Value, ElympicsBehavioursManager.StatePredictability.Both, true);
                behaviourManager.AddStateMetaData(snapshotWithMeta.Value);
            }
            behaviourManager.ApplySnapshot(Replay.Snapshots[firstTick!.Value]);
        }
    }
}
