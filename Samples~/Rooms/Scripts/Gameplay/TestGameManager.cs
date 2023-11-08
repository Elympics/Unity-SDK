using System;
using Elympics;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

public class TestGameManager : ElympicsMonoBehaviour, IClientHandlerGuid, IUpdatable
{
    private readonly static string MenuScene = "RoomsMenu";

    private bool _gameEndedClicked = false;

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
            Elympics.EndGame();

        _gameEndedClicked = false;
    }

    private void LoadMenuScene() => SceneManager.LoadScene(MenuScene);

    [UsedImplicitly] // TODO: remove implicit use from button when OnMatchEnded is fixed
    public void GoBackToMenuAfterShortDelay() => Invoke(nameof(LoadMenuScene), 0.5f);

    public void OnMatchEnded(Guid matchId) => GoBackToMenuAfterShortDelay();
}
