#nullable enable

namespace Elympics.SnapshotAnalysis
{
    /// <summary>Empty manipulator for cases where replay should not be interactive and does not need UI displaying current tick.</summary>
    public class NullReplayManipulator : IReplayManipulator
    {
        public void LoadReplay(IReplayManipulatorClient replayClient, ReplayData data) { }

        public void SetCurrentTick(long tick) { }
    }
}
