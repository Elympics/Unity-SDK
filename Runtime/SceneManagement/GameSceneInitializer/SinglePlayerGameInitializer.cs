using System;
using System.Collections.Generic;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis;
using GameEngineCore;

namespace Elympics
{
    internal class SinglePlayerGameInitializer : GameSceneInitializer
    {
        private SinglePlayerGameEngine _gameEngine;
        public override void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            var matchData = LobbyRegister.GetMatchData() ?? new MatchmakingFinishedData(Guid.Empty, string.Empty, string.Empty, string.Empty, Array.Empty<byte>(), Array.Empty<float>(), string.Empty, string.Empty, new[] { Guid.Empty });
            var userData = CreateUserData(matchData, gameConfig);

            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            var gameEngineAdapter = new GameEngineAdapter(gameConfig);
            ElympicsLogger.State.SetGameMode("single player");
            _gameEngine = new SinglePlayerGameEngine(gameEngineAdapter, behavioursManager, matchData.MatchId);
            server.InitializeInternal(gameConfig,
                gameEngineAdapter,
                behavioursManager,
                new SinglePlayerPlayerHandler(server, gameEngineAdapter, behavioursManager),
                new SinglePlayerSnapshotAnalysisCollector(),
                new DefaultServerElympicsUpdateLoop(behavioursManager, gameEngineAdapter, server, gameConfig),
                handlingBotsOverride: true,
                handlingClientsOverride: true);

            gameEngineAdapter.Initialize(new InitialMatchData
            {
                UserData = userData,
                MatchId = matchData.MatchId,
                QueueName = matchData.QueueName,
                RegionName = matchData.RegionName,
                CustomRoomData = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                CustomMatchmakingData = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                // ExternalGameData = matchData.GameEngineData
                ExternalGameData = Array.Empty<byte>()
            });

            behavioursManager.InitializeInternal(server, userData.Count);
        }

        /// <summary>
        /// Exactly one real player at index 0 (from <paramref name="matchData"/>), followed by the bots
        /// from <see cref="ElympicsGameConfig.TestPlayers"/>, clamped to <see cref="ElympicsGameConfig.MaxPlayers"/>.
        /// Non-bot test players are ignored, as the real player always comes from the match data.
        /// </summary>
        internal static List<InitialMatchUserData> CreateUserData(MatchmakingFinishedData matchData, ElympicsGameConfig gameConfig)
        {
            var users = new List<InitialMatchUserData>
            {
                new()
                {
                    UserId = matchData.MatchedPlayers.Length > 0 ? matchData.MatchedPlayers[0] : Guid.Empty,
                    IsBot = false,
                    BotDifficulty = 0,
                    MatchmakerData = matchData.MatchmakerData,
                    GameEngineData = matchData.GameEngineData,
                    RoomId = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                    TeamIndex = 0,
                },
            };

            var testPlayers = gameConfig.TestPlayers;
            var ignoredNonBots = 0;
            var droppedBots = 0;
            for (var i = 0; i < testPlayers.Count; i++)
            {
                var testPlayer = testPlayers[i];
                if (!testPlayer.isBot)
                {
                    if (i > 0)
                        ignoredNonBots++;
                    continue;
                }
                if (users.Count >= gameConfig.MaxPlayers)
                {
                    droppedBots++;
                    continue;
                }
                users.Add(new InitialMatchUserData
                {
                    UserId = new Guid(users.Count, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                    IsBot = true,
                    BotDifficulty = testPlayer.botDifficulty,
                    MatchmakerData = testPlayer.matchmakerData,
                    GameEngineData = testPlayer.gameEngineData,
                    RoomId = null,
                    TeamIndex = null,
                });
            }

            if (ignoredNonBots > 0)
                ElympicsLogger.LogWarning($"Single player mode supports exactly one real player. "
                    + $"Ignoring {ignoredNonBots} non-bot entries in \"Test players\" past index 0.");
            if (droppedBots > 0)
                ElympicsLogger.LogWarning($"Ignoring {droppedBots} bots from \"Test players\" "
                    + $"because the match would exceed {nameof(ElympicsGameConfig.MaxPlayers)} ({gameConfig.MaxPlayers}).");
            return users;
        }

        public override void Dispose() => _gameEngine?.Dispose();
    }
}
