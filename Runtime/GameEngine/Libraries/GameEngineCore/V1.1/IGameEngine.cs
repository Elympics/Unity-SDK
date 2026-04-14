#nullable enable
using System;
using System.Collections.Generic;

namespace GameEngineCore.V1._1
{
    public interface IGameEngine
    {
        event Action<byte[], string>? InGameDataForPlayerOnReliableChannelGenerated;
        event Action<byte[], string>? InGameDataForPlayerOnUnreliableChannelGenerated;

        [Obsolete("Not used anywhere")] event Action? GameStarted;

        [Obsolete("Use newer version from V1.3")]
        event Action<MatchResult>? GameEnded;

        [Obsolete("Not used anywhere")] event Action<List<GameEvent>>? GameEventsGathered;

        void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId);
        void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId);
        void OnPlayerConnected(string userId);
        void OnPlayerDisconnected(string userId);

        void Init(IGameEngineLogger logger, InitialMatchData initialMatchData);
        void Tick(long tick);
    }
}
