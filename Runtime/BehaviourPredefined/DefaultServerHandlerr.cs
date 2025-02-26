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
        [Tooltip("Automatically kill Game Server if no player will join in " + nameof(startGameTimeoutSeconds) + " seconds")]
        [SerializeField] private bool autoTerminateServer = true;

        [Tooltip("Automatically kill Game Server if any/all players leave the game.")]
        [SerializeField] protected TerminationOption autoTerminationOnLeft = TerminationOption.Any;

        [Min(180)]
        [SerializeField] private float startGameTimeoutSeconds = 180;

        // ReSharper disable MemberCanBePrivate.Global
        protected int PlayersNumber;
        protected bool GameStarted;
        protected readonly HashSet<ElympicsPlayer> PlayersConnected = new();
        // ReSharper restore MemberCanBePrivate.Global

        private TimeSpan _startGameTimeout;
        private DateTime _waitToStartFinishTime;
        private readonly WaitForSeconds _checkInterval = new(5);

        public virtual void OnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas)
        {
            if (!IsEnabledAndActive)
                return;

            PlayersNumber = initialMatchPlayerDatas.Count;
            var humansPlayers = initialMatchPlayerDatas.Count(x => !x.IsBot);
            ElympicsLogger.Log(
                $"Game initialized for {initialMatchPlayerDatas.Count} players(including {initialMatchPlayerDatas.Count - humansPlayers} bots).");
            ElympicsLogger.Log($"Waiting for {humansPlayers} human players to connect.");

            var sb = new StringBuilder()
                .AppendLine($"MatchId: {initialMatchPlayerDatas.MatchId}")
                .AppendLine($"QueueName: {initialMatchPlayerDatas.QueueName}")
                .AppendLine($"RegionName: :{initialMatchPlayerDatas.RegionName}")
                .AppendLine($"CustomMatchmakingData: {initialMatchPlayerDatas.CustomMatchmakingData?.Count.ToString() ?? "null"}")
                .AppendLine($"CustomRoomData: {(initialMatchPlayerDatas.CustomRoomData != null ? string.Join(", ", initialMatchPlayerDatas.CustomRoomData.Select(x => x.Key)) : "null")}")
                .AppendLine($"ExternalGameData: {initialMatchPlayerDatas.ExternalGameData?.Length.ToString() ?? "null"}");

            foreach (var playerData in initialMatchPlayerDatas)
                _ = sb.AppendLine(
                    $"Player {playerData.UserId} {(playerData.IsBot ? "Bot" : "Human")} room {playerData.RoomId} teamIndex {playerData.TeamIndex}");

            ElympicsLogger.Log(sb.ToString());

            if (!autoTerminateServer)
                return;

            _startGameTimeout = TimeSpan.FromSeconds(startGameTimeoutSeconds);
            _ = StartCoroutine(WaitForGameStartOrEnd());
        }

        private IEnumerator WaitForGameStartOrEnd()
        {
            _waitToStartFinishTime = DateTime.Now + _startGameTimeout;

            while (DateTime.Now < _waitToStartFinishTime)
            {
                if (GameStarted)
                    yield break;

                ElympicsLogger.Log("Not all players connected yet...");
                yield return _checkInterval;
            }
            ElympicsLogger.LogWarning(
                $"Forcing game server to quit because conditions for {nameof(TerminationOption)}.{autoTerminationOnLeft} were met.");
            Elympics.EndGame();
        }

        public virtual void OnPlayerDisconnected(ElympicsPlayer player)
        {
            if (!IsEnabledAndActive)
                return;

            _ = PlayersConnected.Remove(player);
            ElympicsLogger.Log($"Player {player} disconnected.");

            switch (autoTerminationOnLeft)
            {
                case TerminationOption.Any when GameStarted:
                case TerminationOption.All when GameStarted && PlayersConnected.Count == 0:
                    ElympicsLogger.LogWarning($"Forcing game server to quit because {autoTerminationOnLeft} players left.");
                    CloseMatch();
                    break;
                case TerminationOption.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Used for cases after the game has started
        /// </summary>
        protected virtual void CloseMatch() => Elympics.EndGame();

        public virtual void OnPlayerConnected(ElympicsPlayer player)
        {
            if (!IsEnabledAndActive)
                return;

            ElympicsLogger.Log($"Player {player} connected.");

            _ = PlayersConnected.Add(player);

            if (GameStarted)
                OnPlayerRejoined();
            else if (PlayersConnected.Count == PlayersNumber)
                OnGameStarted();
        }

        protected virtual void OnPlayerRejoined() { }
        protected virtual void OnGameStarted()
        {
            GameStarted = true;
            ElympicsLogger.Log("All players have connected.");
        }


        // This Unity event method is necessary for the script to have a checkbox in Inspector.
        // https://forum.unity.com/threads/why-do-some-components-have-enable-disable-checkboxes-in-the-inspector-while-others-dont.390770/#post-2547484
        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        { }
    }
}
