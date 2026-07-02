using System;
using GameEngineCore;
using UnityEngine;

namespace Elympics
{
    internal class SinglePlayerGameEngine : IDisposable
    {
        private readonly IGameEngine _gameEngine;
        private readonly string _url;
        private readonly ElympicsBehavioursManager _behavioursManager;
        private readonly Guid _matchId;
        private const string EndGamePath = "matches/run";

        public SinglePlayerGameEngine(IGameEngine gameEngineAdapter, ElympicsConfig config, ElympicsBehavioursManager behavioursManager, Guid matchId)
        {
            _url = string.Join("/", config.ElympicsApiEndpoint, EndGamePath);
            _behavioursManager = behavioursManager;
            _matchId = matchId;
            _gameEngine = gameEngineAdapter;
            _gameEngine.GameEnded += OnGameEnded;
        }

        private void OnGameEnded(ResultMatchUserDatas obj)
        {
            _behavioursManager.OnMatchEnded(_matchId);
            _behavioursManager.OnDisconnectedByServer();

            ElympicsLogger.Log("SinglePlayer game ended.");

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
            if (obj.IsFailure)
                _ = ElympicsLogger.LogException(obj.Error);
            else if (obj.IsSuccess)
                ElympicsLogger.Log("End Results sent successfully.");
        }
        public void Dispose() => _gameEngine.GameEnded -= OnGameEnded;
    }
}
