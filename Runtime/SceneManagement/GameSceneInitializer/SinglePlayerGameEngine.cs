using System;
using Elympics.Core.Logger;
using GameEngineCore;

namespace Elympics
{
    internal class SinglePlayerGameEngine : IDisposable
    {
        private readonly IGameEngine _gameEngine;
        private readonly ElympicsBehavioursManager _behavioursManager;
        private readonly Guid _matchId;

        public SinglePlayerGameEngine(IGameEngine gameEngineAdapter, ElympicsBehavioursManager behavioursManager, Guid matchId)
        {
            _behavioursManager = behavioursManager;
            _matchId = matchId;
            _gameEngine = gameEngineAdapter;
            _gameEngine.GameEnded += OnGameEnded;
        }

        private void OnGameEnded(ResultMatchUserDatas obj)
        {
            _behavioursManager.OnMatchEnded(_matchId);
            _behavioursManager.OnDisconnectedByServer();

            ElympicsLogger.LogInfo("SinglePlayer game ended.");
        }

        public void Dispose() => _gameEngine.GameEnded -= OnGameEnded;
    }
}
