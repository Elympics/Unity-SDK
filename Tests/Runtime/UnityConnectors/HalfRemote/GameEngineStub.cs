using System;
using System.Collections.Generic;
using GameEngineCore.V1._1;
using GameEngineCore.V1._3;
using IGameEngine = GameEngineCore.V1._3.IGameEngine;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    public class GameEngineStub : IGameEngine
    {
        #region Test methods and events

        public event Action<string> PlayerConnected;
        public event Action<byte[], string> InGameDataFromPlayerReliableReceived;
        public event Action<byte[], string> InGameDataFromPlayerUnreliableReceived;

        public void GenerateInGameDataForPlayerOnReliableChannel(byte[] data, string userId) =>
            InGameDataForPlayerOnReliableChannelGenerated?.Invoke(data, userId);

        public void GenerateInGameDataForPlayerOnUnreliableChannel(byte[] data, string userId) =>
            InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(data, userId);

        #endregion

        public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId)
        {
            InGameDataFromPlayerReliableReceived?.Invoke(data, userId);
        }

        public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId)
        {
            InGameDataFromPlayerUnreliableReceived?.Invoke(data, userId);
        }

        public void OnPlayerConnected(string userId)
        {
            PlayerConnected?.Invoke(userId);
        }

        public void OnPlayerDisconnected(string userId)
        {
            throw new NotImplementedException();
        }

        public void Init(IGameEngineLogger logger, InitialMatchData initialMatchData)
        {
            throw new NotImplementedException();
        }

        public void Tick(long tick)
        {
            throw new NotImplementedException();
        }

        public event Action<byte[], string> InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string> InGameDataForPlayerOnUnreliableChannelGenerated;

        public void Init2(InitialMatchUserDatas initialMatchUserDatas)
        {
            throw new NotImplementedException();
        }

        event Action<ResultMatchUserDatas> IGameEngine.GameEnded
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event Action<MatchResult> GameEngineCore.V1._1.IGameEngine.GameEnded
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event Action GameStarted;
        public event Action<List<GameEvent>> GameEventsGathered;
        public event Action<byte[]> InGameDataForSpectatorsOnReliableChannelGenerated;
        public event Action<byte[]> InGameDataForSpectatorsOnUnreliableChannelGenerated;
    }
}
