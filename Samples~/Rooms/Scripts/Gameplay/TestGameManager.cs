using System;
using Elympics;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

public class TestGameManager : ElympicsMonoBehaviour, IClientHandlerGuid, IUpdatable, IServerHandlerGuid
{
    private const string MenuScene = "RoomsMenu";

    private bool _gameEndedClicked;
    private InitialMatchPlayerDatasGuid _initialMatchPlayerDatas;

    [UsedImplicitly]
    public void SetEndGame() => _gameEndedClicked = true;

    [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
    private void EndGameOnServer() => _gameEndedClicked = true;

    public void ElympicsUpdate()
    {
        if (!_gameEndedClicked)
            return;

        if (Elympics.IsClient)
            EndGameOnServer();
        else if (Elympics.IsServer)
        {
            var result = new ResultMatchPlayerDatas();
            for (var i = 0; i < _initialMatchPlayerDatas.Count; i++)
            {
                result.Add(new ResultMatchPlayerData
                {
                    GameEngineData = Array.Empty<byte>(),
                    MatchmakerData = new[] { i == 0 ? 1.0f : -1.0f }
                });
            }

            Elympics.EndGame(result);
        }

        _gameEndedClicked = false;
    }

    private void LoadMenuScene() => SceneManager.LoadScene(MenuScene);

    [UsedImplicitly] // TODO: remove implicit use from button when OnMatchEnded is fixed
    public void GoBackToMenuAfterShortDelay() => Invoke(nameof(LoadMenuScene), 0.5f);

    public void OnMatchEnded(Guid matchId) => GoBackToMenuAfterShortDelay();

    public void OnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas)
    {
        _initialMatchPlayerDatas = initialMatchPlayerDatas;
    }

    public void OnPlayerDisconnected(ElympicsPlayer player)
    {
    }

    public void OnPlayerConnected(ElympicsPlayer player)
    {
    }
}
