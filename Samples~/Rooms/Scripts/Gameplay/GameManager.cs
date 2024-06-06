using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : ElympicsMonoBehaviour, IClientHandlerGuid, IUpdatable, IServerHandlerGuid
{
    private const string MenuScene = "RoomsMenu";

    private static readonly TimeSpan ShoutdownDeley = TimeSpan.FromSeconds(120);

    private InitialMatchPlayerDatasGuid _initialMatchPlayerDatas;
    private bool _allPlayersInGame;
    private readonly HashSet<ElympicsPlayer> _playersConnected = new();

    private bool _gameEndedClicked;

    private CancellationTokenSource _cancellationTokenSource;

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

    [ContextMenu(nameof(GoBackToMenuAfterShortDelay))]
    public void GoBackToMenuAfterShortDelay() => Invoke(nameof(LoadMenuScene), 0.5f);

    public void OnMatchEnded(Guid matchId) => GoBackToMenuAfterShortDelay();

    public void OnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas)
    {
        _initialMatchPlayerDatas = initialMatchPlayerDatas;
    }
    public void OnDisconnectedByServer()
    {
        GoBackToMenuAfterShortDelay();
    }
    public void OnPlayerDisconnected(ElympicsPlayer player)
    {
        if (!IsEnabledAndActive)
            return;

        _ = _playersConnected.Remove(player);

        _allPlayersInGame = false;
        if (_playersConnected.Count == 0 && !_gameEndedClicked)
        {
            _cancellationTokenSource = new();
            TimeOut(_cancellationTokenSource.Token).Forget();
        }
    }
    private async UniTaskVoid TimeOut(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(ShoutdownDeley, cancellationToken: cancellationToken);
            Elympics.EndGame();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Countdown was canceled.");
        }
    }
    public void OnPlayerConnected(ElympicsPlayer player)
    {
        if (!IsEnabledAndActive)
            return;

        _ = _playersConnected.Add(player);
        _cancellationTokenSource?.Cancel();

        if (_playersConnected.Count != _initialMatchPlayerDatas.Count)
            return;

        _allPlayersInGame = true;
    }

}
