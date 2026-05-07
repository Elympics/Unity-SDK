#nullable enable
using System;
using GameEngineCore.V1._4;

namespace GameEngineCore.V2._0
{
    public interface IGameEngine : V1._4.IGameEngine
    {
        void Initialize(InitialMatchData initialMatchData, bool isReplay);
        event Action<ArraySegment<byte>>? SnapshotDataForReplayGenerated;
        event Action<ArraySegment<byte>>? SnapshotReplayInitialized;
    }
}
