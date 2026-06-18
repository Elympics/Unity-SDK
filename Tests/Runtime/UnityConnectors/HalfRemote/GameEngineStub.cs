using System;
using GameEngineCore;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    public class GameEngineStub : IGameEngine
    {
        #region Test methods and events

        public event Action<string> PlayerConnected;
        public event Action<byte[], string> InGameDataFromPlayerReliableReceived;
        public event Action<byte[], string> InGameDataFromPlayerUnreliableReceived;

        public event Action<ResultMatchUserDatas> GameEnded;
        public event Action Initialized;

        public void Initialize(InitialMatchData initialMatchData) => throw new NotImplementedException();
        public void Initialize(InitialMatchData initialMatchData, bool isReplay) => throw new NotImplementedException();

        public event Action<ArraySegment<byte>> SnapshotDataForReplayGenerated;
        public event Action<ArraySegment<byte>> SnapshotReplayInitialized;

        public void GenerateInGameDataForPlayerOnReliableChannel(byte[] data, string userId) =>
            InGameDataForPlayerOnReliableChannelGenerated?.Invoke(data, userId);

        public void GenerateInGameDataForPlayerOnUnreliableChannel(byte[] data, string userId) =>
            InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(data, userId);

        public void EndGame(ResultMatchUserDatas matchUserData) => GameEnded?.Invoke(matchUserData);

        #endregion

        public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId) => InGameDataFromPlayerReliableReceived?.Invoke(data, userId);

        public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) => InGameDataFromPlayerUnreliableReceived?.Invoke(data, userId);

        public void OnPlayerConnected(string userId) => PlayerConnected?.Invoke(userId);

        public void OnPlayerDisconnected(string userId) => throw new NotImplementedException();

        public void Init(IGameEngineLogger logger, InitialMatchData initialMatchData) => throw new NotImplementedException();

        public void Tick(long tick) => throw new NotImplementedException();

        public event Action<byte[], string> InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string> InGameDataForPlayerOnUnreliableChannelGenerated;

        public event Action<byte[]> InGameDataForSpectatorsOnReliableChannelGenerated;
        public event Action<byte[]> InGameDataForSpectatorsOnUnreliableChannelGenerated;
    }
}
