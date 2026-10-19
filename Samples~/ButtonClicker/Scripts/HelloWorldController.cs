using System;
using System.Linq;
using Elympics;
using JetBrains.Annotations;
using MatchTcpClients.Synchronizer;
using UnityEngine;
using UnityEngine.UI;

public class HelloWorldController : ElympicsMonoBehaviour, IInitializable, IInputHandler, IUpdatable, IClientHandlerGuid, IServerHandlerGuid
{
    [SerializeField] private int secondsLimit = 15;
    [SerializeField] private Text mainLabel;
    [SerializeField] private Text[] playerLabels;

    private ElympicsArray<ElympicsInt> _clickCount;
    private ElympicsInt _ticksLeft;

    private int _playerCount;
    // A match can have fewer players than the config allows, and its result needs one entry per player.
    private int _matchPlayerCount;
    private bool _clicked;
    private int _lastClickedCached;
    private bool _initialized;
    private bool _ended;

    public void Initialize()
    {
        var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
        _playerCount = config.MaxPlayers;
        if (playerLabels.Length < _playerCount)
        {
            Debug.LogError(mainLabel.text = "Player count too high");
            return;
        }

        _clickCount = new ElympicsArray<ElympicsInt>(_playerCount, () => new ElympicsInt());
        mainLabel.text = $"Game started for {_playerCount} players";
        _ticksLeft = new ElympicsInt((int)Mathf.Ceil(secondsLimit * Elympics.TicksPerSecond));
        _initialized = true;
    }

    [UsedImplicitly]
    public void OnClick() => _clicked = true;

    public void OnInputForClient(IInputWriter inputSerializer)
    { }

    public void OnInputForBot(IInputWriter inputSerializer)
    { }

    [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
    private void IncrementCounter(int playerId)
    {
        _clickCount.Values[playerId].Value++;
        playerLabels[playerId].gameObject.SetActive(true);

        if (_clickCount.Values[playerId].Value == 5)
            LogOnPlayers($"Player {playerId} just scored 5 points");
    }

    [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
    private void LogOnPlayers(string message)
    {
        Debug.LogError("RPC message from server: " + message);
    }

    public void ElympicsUpdate()
    {
        if (!_initialized)
            return;
        _ticksLeft.Value--;
        if (_ticksLeft.Value <= 0)
        {
            if (Elympics.IsServer)
                Elympics.EndGame(new ResultMatchPlayerDatas(_clickCount.Values.Take(_matchPlayerCount).Select(
                    x => new ResultMatchPlayerData { MatchmakerData = new float[] { x.Value } }).ToList()));
            return;
        }

        if (_clicked && Elympics.IsClient)
        {
            IncrementCounter(Math.Max((int)Elympics.Player, 0));
            _clicked = false;
        }
    }

    public void Update()
    {
        if (!_initialized)
            return;
        if (!_ended)
        {
            var secondsLeft = _ticksLeft.Value / (float)Elympics.TicksPerSecond;
            mainLabel.text = $"Game started for {_playerCount} players (time left: {secondsLeft:n1}s)";
        }
        else
            mainLabel.text = $"Game ended for {_playerCount} players";

        for (var playerId = 0; playerId < _playerCount; playerId++)
            playerLabels[playerId].text = $"Player {playerId} clicked {_clickCount.Values[playerId].Value} times";
    }

    public void OnMatchEnded(Guid matchId)
    {
        _ended = true;
    }

    public void OnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas) => _matchPlayerCount = initialMatchPlayerDatas.Count;

    public void OnPlayerDisconnected(ElympicsPlayer player)
    { }

    public void OnPlayerConnected(ElympicsPlayer player)
    { }
}
