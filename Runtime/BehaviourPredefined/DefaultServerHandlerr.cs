using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Elympics
{
    public class DefaultServerHandlerr : ElympicsMonoBehaviour, IServerHandlerGuid
    {
        [SerializeField] private float startGameTimeoutSeconds = 30;
        private TimeSpan _startGameTimeout;

        private int _playersNumber;
        private DateTime _waitToStartFinishTime;
        private bool _gameStarted;

        private readonly HashSet<ElympicsPlayer> _playersConnected = new();

        public void OnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas)
        {
            if (!IsEnabledAndActive)
                return;

            _startGameTimeout = TimeSpan.FromSeconds(startGameTimeoutSeconds);
            _playersNumber = initialMatchPlayerDatas.Count;
            var humansPlayers = initialMatchPlayerDatas.Count(x => !x.IsBot);
            ElympicsLogger.Log($"Game initialized for {initialMatchPlayerDatas.Count} players "
                               + $"(including {initialMatchPlayerDatas.Count - humansPlayers} bots).");
            ElympicsLogger.Log($"Waiting for {humansPlayers} human players to connect.");
            var sb = new StringBuilder()
                .AppendLine($"MatchId: {initialMatchPlayerDatas.MatchId}")
                .AppendLine($"QueueName: {initialMatchPlayerDatas.QueueName}")
                .AppendLine($"RegionName: :{initialMatchPlayerDatas.RegionName}")
                .AppendLine($"CustomMatchmakingData: {initialMatchPlayerDatas.CustomMatchmakingData?.Count.ToString() ?? "null"}")
                .AppendLine($"CustomRoomData: {(initialMatchPlayerDatas.CustomRoomData != null ? string.Join(", ", initialMatchPlayerDatas.CustomRoomData.Select(x => x.Key)) : "null")}")
                .AppendLine($"ExternalGameData: {initialMatchPlayerDatas.ExternalGameData?.Length.ToString() ?? "null"}");

            foreach (var playerData in initialMatchPlayerDatas)
                _ = sb.AppendLine($"Player {playerData.UserId} {(playerData.IsBot ? "Bot" : "Human")} room {playerData.RoomId} teamIndex {playerData.TeamIndex}");
            ElympicsLogger.Log(sb.ToString());

            _ = StartCoroutine(WaitForGameStartOrEnd());
        }

        private IEnumerator WaitForGameStartOrEnd()
        {
            _waitToStartFinishTime = DateTime.Now + _startGameTimeout;

            while (DateTime.Now < _waitToStartFinishTime)
            {
                if (_gameStarted)
                    yield break;

                ElympicsLogger.Log("Not all players connected yet...");
                yield return new WaitForSeconds(5);
            }

            ElympicsLogger.LogWarning("Forcing game server to quit because some players did not connect on time.\n"
                                      + "Connected players: "
                                      + string.Join(", ", _playersConnected));
            Elympics.EndGame();
        }

        public void OnPlayerDisconnected(ElympicsPlayer player)
        {
            if (!IsEnabledAndActive)
                return;

            ElympicsLogger.Log($"Player {player} disconnected.");
            ElympicsLogger.LogWarning("Forcing game server to quit because one of the players disconnected.");
            Elympics.EndGame();
        }

        public void OnPlayerConnected(ElympicsPlayer player)
        {
            if (!IsEnabledAndActive)
                return;

            ElympicsLogger.Log($"Player {player} connected.");

            _ = _playersConnected.Add(player);
            if (_playersConnected.Count != _playersNumber || _gameStarted)
                return;

            _gameStarted = true;
            ElympicsLogger.Log("All players have connected.");
        }

        // This Unity event method is necessary for the script to have a checkbox in Inspector.
        // https://forum.unity.com/threads/why-do-some-components-have-enable-disable-checkboxes-in-the-inspector-while-others-dont.390770/#post-2547484
        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        {
        }
    }
}
