#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Behaviour;
using Elympics.SnapshotAnalysis.Serialization;
using Plugins.Elympics.Runtime.SnapshotAnalysis.Utils;
using UnityEngine;

namespace Elympics.SnapshotAnalysis
{
    internal abstract class SnapshotAnalysisCollector : IDisposable
    {
        protected SnapshotSerializer SnapshotSerializer = null!;
        protected readonly SnapshotSerializationPackage Package = new();
        private const int ChunkLimit = 1000;
        private readonly ElympicsSnapshotWithMetadata[] _buffer1 = new ElympicsSnapshotWithMetadata[ChunkLimit];
        private readonly ElympicsSnapshotWithMetadata[] _buffer2 = new ElympicsSnapshotWithMetadata[ChunkLimit];
        private int _index;
        private int _currentBuffer = 1;
        private bool _disposed;
        public void Initialize(ElympicsGameConfig gameConfig, InitialMatchPlayerDatasGuid initialMatchPlayerData, SnapshotSerializer serializer)
        {
            var initData = new SnapshotSaverInitData(
                SnapshotAnalysisUtils.Version,
                gameConfig.GameName,
                gameConfig.gameId,
                gameConfig.gameVersion,
                gameConfig.Players,
                ElympicsConfig.SdkVersion,
                gameConfig.TickDuration,
                new CollectorMatchData
                {
                    MatchId = initialMatchPlayerData.MatchId,
                    QueueName = initialMatchPlayerData.QueueName,
                    RegionName = initialMatchPlayerData.RegionName,
                    CustomRoomData = initialMatchPlayerData.CustomRoomData?.ToDictionary(x => x.Key,
                        x => (IDictionary<string, string>)new Dictionary<string, string>(x.Value)),
                    CustomMatchmakingData = initialMatchPlayerData.CustomMatchmakingData?.ToDictionary(x => x.Key, x => x.Value),
                },
                initialMatchPlayerData.ExternalGameData,
                new List<InitialMatchPlayerDataGuid>(initialMatchPlayerData)
            );
            SnapshotSerializer = serializer;
            SaveInitData(initData);
        }

        public abstract void CaptureSnapshot(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata snapshot);

        protected void StoreToBuffer(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata currentSnapshot)
        {
            if (_disposed)
                return;

            var buffer = GetBuffer;
            var snapshotToStore = currentSnapshot;
            if (previousSnapshot is not null)
            {
                var copyCreated = false;
                using (ElympicsMarkers.Elympics_SnapshotCollector_BufferStore.Auto())
                {
                    var finder = new NetworkBehaviourFinder(previousSnapshot, currentSnapshot);
                    foreach (var behaviourPair in finder)
                    {
                        if (!behaviourPair.DataFromFirst.SequenceEqual(behaviourPair.DataFromSecond))
                            continue;

                        if (!copyCreated)
                        {
                            snapshotToStore = new ElympicsSnapshotWithMetadata(currentSnapshot, currentSnapshot.TickEndUtc);
                            snapshotToStore.Data = new List<KeyValuePair<int, byte[]>>(snapshotToStore.Data);
                            copyCreated = true;
                        }
                        snapshotToStore.Data[behaviourPair.IndexFromSecond] = new KeyValuePair<int, byte[]>(behaviourPair.NetworkId, null!);
                    }
                }
            }
            buffer[_index] = snapshotToStore;
            ++_index;
            if (_index != ChunkLimit)
                return;
            _currentBuffer = GetNewBufferNumber();
            _index = 0;
            using (ElympicsMarkers.Elympics_SnapshotCollector_OnBufferLimit.Auto())
                OnBufferLimit(buffer).Forget();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_index > 0)
            {
                var buffer = GetBuffer;
                var array = new ElympicsSnapshotWithMetadata[_index];
                Array.Copy(buffer, array, _index);
                Debug.Log($"Last Tick to save is {array[^1].Tick}");
                SaveLastDataAndDispose(array);
            }

            Debug.Log($"{nameof(SnapshotAnalysisCollector)} Disposed");
        }


        protected abstract void SaveInitData(SnapshotSaverInitData initData);
        protected abstract UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer);
        protected abstract void SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots);

        protected ElympicsSnapshotWithMetadata[] GetBuffer => _currentBuffer switch
        {
            1 => _buffer1,
            2 => _buffer2,
            _ => throw new ArgumentOutOfRangeException()
        };

        private int GetNewBufferNumber() => _currentBuffer switch
        {
            1 => 2,
            2 => 1,
            _ => throw new InvalidOperationException()
        };
    }
}
