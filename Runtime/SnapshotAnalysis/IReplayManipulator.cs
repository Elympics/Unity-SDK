using System.Collections.Generic;

namespace Elympics.SnapshotAnalysis
{
    public interface IReplayManipulator
    {
        void LoadReplay(IReplayManipulatorClient replayClient, ReplayData data);
        /// <summary>Drops the replay given to <see cref="LoadReplay"/>, whose match is being torn down. The manipulator must not keep its client.</summary>
        void UnloadReplay();
        void SetCurrentTick(long tick);
    }

    public interface IReplayManipulatorClient
    {
        void SetIsPlaying(bool isPlaying);
        void JumpToTick(long tick);
    }

    public readonly struct ReplayData
    {
        public SnapshotSaverInitData InitData { get; init; }
        public Dictionary<long, ElympicsSnapshotWithMetadata> Snapshots { get; init; }
    }
}
