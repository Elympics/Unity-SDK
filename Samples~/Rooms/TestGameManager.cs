using System;
using Elympics;
using UnityEngine.SceneManagement;

public class TestGameManager : ElympicsMonoBehaviour, IClientHandlerGuid, IUpdatable
{
    private bool gameShouldEnd = false;
    public void SetEndGame() => gameShouldEnd = true;

    private void EndGame()
    {
        if (Elympics.IsServer) Elympics.EndGame();
        else EndGameOnServer();
    }

    [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
    private void EndGameOnServer() => Elympics.EndGame();

    public void OnMatchEnded(Guid matchId) => Invoke(nameof(LoadMenuScene), 5f);

    private void LoadMenuScene() => SceneManager.LoadScene(0);

    public void ElympicsUpdate()
    {
        if (gameShouldEnd) EndGame();
    }
}
