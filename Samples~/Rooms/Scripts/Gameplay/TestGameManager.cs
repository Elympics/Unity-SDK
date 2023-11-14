using System;
using Elympics;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

public class TestGameManager : ElympicsMonoBehaviour, IClientHandlerGuid, IUpdatable
{
    private bool gameShouldEnd = false;

    [UsedImplicitly]
    public void SetEndGame() => EndGameOnServer();

    [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
    private void EndGameOnServer() => gameShouldEnd = true;

    public void OnMatchEnded(Guid matchId) => Invoke(nameof(LoadMenuScene), 2f);

    private void LoadMenuScene() => SceneManager.LoadScene(0);

    public void ElympicsUpdate()
    {
        if (Elympics.IsServer && gameShouldEnd)
            Elympics.EndGame();
    }
}
