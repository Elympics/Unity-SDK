using System;
using GameBotCore.V1._1;
using GameEngineCore.V1._1;

namespace UnityConnectors
{
    public class GameEngineGameBotAdapterConnector : IDisposable
    {
        private readonly IGameEngine _gameEngine;
        private readonly IGameBot _gameBotAdapter;
        private readonly BotConfiguration _botConfiguration;

        public GameEngineGameBotAdapterConnector(IGameEngine gameEngine, IGameBot gameBotAdapter, BotConfiguration botConfiguration)
        {
            _gameEngine = gameEngine;
            _gameBotAdapter = gameBotAdapter;
            _botConfiguration = botConfiguration;

            AddCallbacks();
        }

        private void AddCallbacks()
        {
            _gameEngine.InGameDataForPlayerOnReliableChannelGenerated += OnInGameDataReliableReceived;
            _gameEngine.InGameDataForPlayerOnUnreliableChannelGenerated += OnInGameDataUnreliableReceived;

            _gameBotAdapter.InGameDataForReliableChannelGenerated += ProcessBotAdapterReliableMessage;
            _gameBotAdapter.InGameDataForUnreliableChannelGenerated += ProcessBotAdapterUnreliableMessage;
        }

        private void RemoveCallbacks()
        {
            _gameEngine.InGameDataForPlayerOnReliableChannelGenerated -= OnInGameDataReliableReceived;
            _gameEngine.InGameDataForPlayerOnUnreliableChannelGenerated -= OnInGameDataUnreliableReceived;

            _gameBotAdapter.InGameDataForReliableChannelGenerated -= ProcessBotAdapterReliableMessage;
            _gameBotAdapter.InGameDataForUnreliableChannelGenerated -= ProcessBotAdapterUnreliableMessage;
        }

        private void OnInGameDataReliableReceived(byte[] data, string userId)
        {
            if (InGameDataIntendedForBot(userId))
                _gameBotAdapter.OnInGameDataReliableReceived(data);
        }

        private void OnInGameDataUnreliableReceived(byte[] data, string userId)
        {
            if (InGameDataIntendedForBot(userId))
                _gameBotAdapter.OnInGameDataUnreliableReceived(data);
        }

        private bool InGameDataIntendedForBot(string userId) => _botConfiguration.UserId == userId;
        private void ProcessBotAdapterReliableMessage(byte[] data) => _gameEngine.OnInGameDataFromPlayerReliableReceived(data, _botConfiguration.UserId);
        private void ProcessBotAdapterUnreliableMessage(byte[] data) => _gameEngine.OnInGameDataFromPlayerUnreliableReceived(data, _botConfiguration.UserId);

        public void Dispose()
        {
            if (_gameBotAdapter == null || _gameEngine == null)
                return;
            RemoveCallbacks();
        }
    }
}
