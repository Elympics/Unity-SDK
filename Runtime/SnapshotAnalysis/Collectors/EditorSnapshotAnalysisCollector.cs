#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis.Serialization;

namespace Elympics.SnapshotAnalysis
{
    internal class EditorSnapshotAnalysisCollector : SnapshotAnalysisCollector
    {
        public static readonly string DefaultFileExtension = ".snapshots";
        private readonly FileStream? _stream;

        private bool _initDataSaved;

        public EditorSnapshotAnalysisCollector(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                ElympicsLogger.LogError("Path for saving snapshots is empty, snapshot saving will be disabled.");
                _stream = null;
                return;
            }

            try
            {
                //If path ends with directory separator (ex. "C:\Users\Elympics\Snapshots\"), append auto generated file name to it
                if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
                    path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + DefaultFileExtension);
                else if (!path.EndsWith(DefaultFileExtension))
                    path += DefaultFileExtension;

                var directoryPath = Path.GetDirectoryName(path);

                //Make sure all directories in path exist
                if (!string.IsNullOrWhiteSpace(directoryPath))
                    _ = Directory.CreateDirectory(directoryPath);

                _stream = new FileStream(path, FileMode.Create);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogError($"Failed to open stream to the snapshot save file with path \"{path}\". Snapshot saving will be disabled. Reason:\n{e}");
                _stream = null;
            }
        }

        public override void CaptureSnapshot(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata snapshot) => StoreToBuffer(previousSnapshot, snapshot);
        protected override void SaveInitData(SnapshotSaverInitData initData)
        {
            if (_stream == null)
                return;

            if (_initDataSaved)
                throw new InvalidOperationException("Init data is already saved.");

            _initDataSaved = true;

            SerializeInitDataAsync(SerializationUtil.LatestVersion, initData).Forget();
        }

        private async UniTaskVoid SerializeInitDataAsync(string version, SnapshotSaverInitData initData)
        {
            await SnapshotSerializer.SerializeVersionToStream(_stream, version);
            await SnapshotSerializer.SerializeToStream(_stream, initData);
        }
        protected override async UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer)
        {
            if (_stream == null)
                return;
            if (!_initDataSaved)
                throw new InvalidOperationException(
                    $"{nameof(SaveInitData)} has to be called once to initialize {nameof(EditorSnapshotAnalysisCollector)} before {nameof(CaptureSnapshot)} can be called.");

            Package.Snapshots = buffer;
            await SnapshotSerializer.SerializeToStream(_stream, Package);
        }

        protected override void SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots)
        {
            Package.Snapshots = snapshots;
            SnapshotSerializer.SerializeToStream(_stream, Package).Wait();
            _stream?.Dispose();
        }
    }
}
