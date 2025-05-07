#nullable enable
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
namespace Elympics.SnapshotAnalysis
{
    internal class ServerOnlineSnapshotAnalysisCollector : SnapshotAnalysisCollector
    {
        private readonly GameEngineAdapter _gameEngineAdapter;
        private readonly ElympicsBase _elympicsBase;
        private readonly ElympicsBehavioursManager _elympicsBehavioursManager;
        private readonly MemoryStream _memoryStream = new();
        public ServerOnlineSnapshotAnalysisCollector(GameEngineAdapter gameEngineAdapter, ElympicsBase elympicsBase, ElympicsBehavioursManager elympicsBehavioursManager)
        {
            _gameEngineAdapter = gameEngineAdapter;
            _elympicsBase = elympicsBase;
            _elympicsBehavioursManager = elympicsBehavioursManager;
        }

        private async UniTaskVoid SerializeAndSendInitData(SnapshotSaverInitData initData)
        {
            ResetMemoryStream(_memoryStream);
            await SnapshotSerializer.SerializeVersionToStream(_memoryStream, initData.SnapshotSaverVersion);
            await SnapshotSerializer.SerializeToStream(_memoryStream, initData);
            if (_memoryStream.TryGetBuffer(out var buffer))
                _gameEngineAdapter.SaveReplayInitData(buffer);
        }
        private async UniTask SerializeAndSendSnapshot(ElympicsSnapshotWithMetadata[] snapshotChunk)
        {
            Package.Snapshots = snapshotChunk;
            ResetMemoryStream(_memoryStream);
            await SnapshotSerializer.SerializeToStream(_memoryStream, Package);
            if (_memoryStream.TryGetBuffer(out var buffer))
                _gameEngineAdapter.SaveSnapshotForReplay(buffer);
        }
        public override void CaptureSnapshot(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata snapshot) => StoreToBuffer(previousSnapshot, snapshot);
        protected override void SaveInitData(SnapshotSaverInitData initData) => SerializeAndSendInitData(initData).Forget();
        protected override async UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer) => await SerializeAndSendSnapshot(buffer);
        protected override async ValueTask SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots) => await SerializeAndSendSnapshot(snapshots);

        private void ResetMemoryStream(MemoryStream stream)
        {
            _ = stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(0);
        }
    }
}
