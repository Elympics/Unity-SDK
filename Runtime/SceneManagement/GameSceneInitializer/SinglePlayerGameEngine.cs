using System;
using Elympics.ElympicsSystems.Internal;
using GameEngineCore.V1._3;
using UnityEngine;
using IGameEngine = GameEngineCore.V1._4.IGameEngine;

namespace Elympics
{
    internal class SinglePlayerGameEngine : IDisposable
    {
        private readonly IGameEngine _gameEngine;
        private readonly ElympicsLoggerContext _logger;
        private readonly string _url;
        private readonly ElympicsBehavioursManager _behavioursManager;
        private readonly Guid _matchId;
        private const string EndGamePath = "matches/run";
        public SinglePlayerGameEngine(IGameEngine gameEngineAdapter, ElympicsConfig config, ElympicsLoggerContext logger, ElympicsBehavioursManager behavioursManager, Guid matchId)
        {
            _logger = logger.WithContext(nameof(SinglePlayerGameEngine));
            _url = string.Join("/", config.ElympicsApiEndpoint, EndGamePath);
            _behavioursManager = behavioursManager;
            _matchId = matchId;
            _gameEngine = gameEngineAdapter;
            _gameEngine.GameEnded += OnGameEnded;
        }
        private void OnGameEnded(ResultMatchUserDatas obj)
        {
            var logger = _logger.WithMethodName();

            _behavioursManager.OnMatchEnded(_matchId);
            _behavioursManager.OnDisconnectedByServer();

            logger.Log("SinglePlayer game ended.");

            if (Application.isEditor)
                return;

            //TO DO: uncomment this once ready
            // var requestData = new MatchEndedRequestDTO
            // {
            //     matchId = ElympicsLobbyClient.Instance!.MatchDataGuid!.MatchId.ToString(),
            // };
            //
            // var jwt = ElympicsLobbyClient.Instance.AuthData?.BearerAuthorization ?? throw new ElympicsException("User is not authenticated.");
            //
            // ElympicsWebClient.SendPostRequest<MatchEndedResponseDTO>(_url, requestData, jwt, Callback);
        }
        private void Callback(Result<MatchEndedResponseDTO, Exception> obj)
        {
            var logger = _logger.WithMethodName();
            if (obj.IsFailure)
                logger.Exception(obj.Error);
            else if (obj.IsSuccess)
                logger.Log("End Results sent successfully.");
        }
        public void Dispose() => _gameEngine.GameEnded -= OnGameEnded;
    }
}
