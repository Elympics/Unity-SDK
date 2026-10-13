#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis.Serialization;

namespace Elympics.SnapshotAnalysis
{
    internal class EditorSnapshotAnalysisCollector : SnapshotAnalysisCollector
    {
        public static readonly string DefaultFileExtension = ".snapshots";
        private readonly FileStream? _stream;

        private bool _initDataSaved;
        /// <summary>All writes, chained, so none overlaps another and the stream is closed only after the last one.</summary>
        private Task _writes = Task.CompletedTask;

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

            var version = SerializationUtil.LatestVersion;
            EnqueueWrite(async stream =>
            {
                await SnapshotSerializer.SerializeVersionToStream(stream, version).ConfigureAwait(false);
                await SnapshotSerializer.SerializeToStream(stream, initData).ConfigureAwait(false);
            });
        }

        protected override UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer)
        {
            if (_stream == null)
                return default;
            if (!_initDataSaved)
                throw new InvalidOperationException(
                    $"{nameof(SaveInitData)} has to be called once to initialize {nameof(EditorSnapshotAnalysisCollector)} before {nameof(CaptureSnapshot)} can be called.");

            // The buffer is reused once the other one fills up, so write a copy.
            var snapshots = (ElympicsSnapshotWithMetadata[])buffer.Clone();
            EnqueueWrite(stream => SerializeSnapshots(stream, snapshots));
            return default;
        }

        protected override void SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots)
        {
            if (_stream == null)
                return;
            EnqueueWrite(stream => SerializeSnapshots(stream, snapshots));
            try
            {
                // Also on an aborted match: in-flight writes must finish before the stream is closed.
                _writes.Wait();
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
            _stream.Dispose();
        }

        private Task SerializeSnapshots(Stream stream, ElympicsSnapshotWithMetadata[] snapshots) =>
            SnapshotSerializer.SerializeToStream(stream, new SnapshotSerializationPackage { Snapshots = snapshots });

        private void EnqueueWrite(Func<Stream, Task> write)
        {
            var stream = _stream!;
            var previous = _writes;
            _writes = Chain();

            async Task Chain()
            {
                try
                {
                    await previous.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogException(e);
                }
                await write(stream).ConfigureAwait(false);
            }
        }
    }
}
