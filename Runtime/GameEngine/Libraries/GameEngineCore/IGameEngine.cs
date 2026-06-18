#nullable enable
using System;

namespace GameEngineCore
{
    public interface IGameEngine
    {
        event Action? Initialized;
        void Initialize(InitialMatchData initialMatchData);
        void Initialize(InitialMatchData initialMatchData, bool isReplay);

        event Action<byte[], string>? InGameDataForPlayerOnReliableChannelGenerated;
        event Action<byte[], string>? InGameDataForPlayerOnUnreliableChannelGenerated;
        event Action<byte[]>? InGameDataForSpectatorsOnReliableChannelGenerated;
        event Action<byte[]>? InGameDataForSpectatorsOnUnreliableChannelGenerated;
        void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId);
        void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId);

        void OnPlayerConnected(string userId);
        void OnPlayerDisconnected(string userId);

        void Tick(long tick);

        event Action<(Guid UserId, float Score, DateTimeOffset Time)>? IntermediateScoreSubmitted;

        event Action<ResultMatchUserDatas?>? GameEnded;

        event Action<ArraySegment<byte>>? SnapshotDataForReplayGenerated;
        event Action<ArraySegment<byte>>? SnapshotReplayInitialized;
    }
}
