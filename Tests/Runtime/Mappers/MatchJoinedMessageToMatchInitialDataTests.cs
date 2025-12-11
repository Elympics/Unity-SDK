using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Authentication.Models;
using Elympics.Mappers;
using MatchTcpModels.Messages;
using NUnit.Framework;

namespace Elympics.Tests.Mappers
{
    public class MatchJoinedMessageToMatchInitialDataTests
    {
        private const string ValidMatchId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
        private const string ValidUserId = "12345678-1234-1234-1234-123456789012";
        private const string ValidRoomId = "87654321-4321-4321-4321-210987654321";

        [Test]
        public void Map_WithValidMinimalData_ShouldMapCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.AreEqual(Guid.Parse(ValidMatchId), result.MatchId);
            Assert.IsFalse(result.IsReplay);
            Assert.AreEqual("TestQueue", result.QueueName);
            Assert.AreEqual("EU", result.RegionName);
            Assert.IsNotNull(result.CustomRoomData);
            Assert.AreEqual(0, result.CustomRoomData.Count);
            Assert.IsNotNull(result.CustomMatchmakingData);
            Assert.AreEqual(0, result.CustomMatchmakingData.Count);
            Assert.IsNull(result.ExternalGameData);
            Assert.IsNotNull(result.PlayerInitialDatas);
            Assert.AreEqual(0, result.PlayerInitialDatas.Count);
        }

        [Test]
        public void Map_WithSinglePlayerAndNoCustomData_ShouldMapPlayerCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = ValidUserId,
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = new byte[] { 1, 2, 3 },
                        MatchmakerData = new[] { 1.5f, 2.5f },
                        RoomId = ValidRoomId,
                        TeamIndex = 0,
                        Nickname = "TestPlayer",
                        NicknameType = "Common",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    }
                }
            };

            var result = message.Map();

            Assert.AreEqual(1, result.PlayerInitialDatas.Count);
            var player = result.PlayerInitialDatas.First();
            Assert.AreEqual(ElympicsPlayer.FromIndex(0), player.Player);
            Assert.AreEqual(Guid.Parse(ValidUserId), player.UserId);
            Assert.IsFalse(player.IsBot);
            Assert.AreEqual(0.0, player.BotDifficulty);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, player.GameEngineData);
            CollectionAssert.AreEqual(new float[] { 1.5f, 2.5f }, player.MatchmakerData);
            Assert.AreEqual(Guid.Parse(ValidRoomId), player.RoomId);
            Assert.AreEqual(0u, player.TeamIndex);
            Assert.AreEqual("TestPlayer", player.Nickname);
            Assert.AreEqual(NicknameType.Common, player.NicknameType);
            Assert.IsNotNull(player.CustomData);
            Assert.AreEqual(0, player.CustomData.Count);
        }

        [Test]
        public void Map_WithSinglePlayerAndCustomData_ShouldMapPlayerCustomDataCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = ValidUserId,
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = Array.Empty<byte>(),
                        MatchmakerData = Array.Empty<float>(),
                        RoomId = ValidRoomId,
                        TeamIndex = 0,
                        Nickname = "TestPlayer",
                        NicknameType = "Verified",
                        CustomDataKeys = new[] { "key1", "key2" },
                        CustomDataValues = new[] { "value1", "value2" }
                    }
                }
            };

            var result = message.Map();

            var player = result.PlayerInitialDatas.First();
            Assert.AreEqual(NicknameType.Verified, player.NicknameType);
            Assert.IsNotNull(player.CustomData);
            Assert.AreEqual(2, player.CustomData.Count);
            Assert.AreEqual("value1", player.CustomData["key1"]);
            Assert.AreEqual("value2", player.CustomData["key2"]);
        }

        [Test]
        public void Map_WithMultiplePlayers_ShouldMapAllPlayersCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = ValidUserId,
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = Array.Empty<byte>(),
                        MatchmakerData = Array.Empty<float>(),
                        RoomId = ValidRoomId,
                        TeamIndex = 0,
                        Nickname = "Player1",
                        NicknameType = "Common",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    },
                    new()
                    {
                        UserId = "98765432-9876-9876-9876-987654321098",
                        IsBot = true,
                        BotDifficulty = 0.75,
                        GameEngineData = Array.Empty<byte>(),
                        MatchmakerData = Array.Empty<float>(),
                        RoomId = ValidRoomId,
                        TeamIndex = 1,
                        Nickname = "Bot1",
                        NicknameType = "Undefined",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    }
                }
            };

            var result = message.Map();

            Assert.AreEqual(2, result.PlayerInitialDatas.Count);
            var players = result.PlayerInitialDatas.ToList();

            Assert.AreEqual(ElympicsPlayer.FromIndex(0), players[0].Player);
            Assert.AreEqual("Player1", players[0].Nickname);
            Assert.IsFalse(players[0].IsBot);

            Assert.AreEqual(ElympicsPlayer.FromIndex(1), players[1].Player);
            Assert.AreEqual("Bot1", players[1].Nickname);
            Assert.IsTrue(players[1].IsBot);
            Assert.AreEqual(0.75, players[1].BotDifficulty);
            Assert.AreEqual(NicknameType.Undefined, players[1].NicknameType);
        }

        [Test]
        public void Map_WithCustomMatchmakingData_ShouldMapCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = new[] { "mmKey1", "mmKey2" },
                CustomMatchmakingDataValues = new[] { "mmValue1", "mmValue2" },
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.IsNotNull(result.CustomMatchmakingData);
            Assert.AreEqual(2, result.CustomMatchmakingData.Count);
            Assert.AreEqual("mmValue1", result.CustomMatchmakingData["mmKey1"]);
            Assert.AreEqual("mmValue2", result.CustomMatchmakingData["mmKey2"]);
        }

        [Test]
        public void Map_WithSingleRoomAndNoCustomData_ShouldMapRoomCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = new[] { ValidRoomId },
                CustomRoomDataNumberPerRoom = new[] { 0 },
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.AreEqual(1, result.CustomRoomData.Count);
            var roomId = Guid.Parse(ValidRoomId);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(roomId));
            Assert.AreEqual(0, result.CustomRoomData[roomId].Count);
        }

        [Test]
        public void Map_WithSingleRoomAndCustomData_ShouldMapRoomCustomDataCorrectly()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = new[] { ValidRoomId },
                CustomRoomDataNumberPerRoom = new[] { 2 },
                CustomRoomDataKeys = new[] { "roomKey1", "roomKey2" },
                CustomRoomDataValues = new[] { "roomValue1", "roomValue2" },
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            var roomId = Guid.Parse(ValidRoomId);
            Assert.AreEqual(1, result.CustomRoomData.Count);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(roomId));
            var roomData = result.CustomRoomData[roomId];
            Assert.AreEqual(2, roomData.Count);
            Assert.AreEqual("roomValue1", roomData["roomKey1"]);
            Assert.AreEqual("roomValue2", roomData["roomKey2"]);
        }

        [Test]
        public void Map_WithMultipleRoomsAndCustomData_ShouldMapAllRoomsCorrectly()
        {
            var room1Id = "11111111-1111-1111-1111-111111111111";
            var room2Id = "22222222-2222-2222-2222-222222222222";

            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = new[] { room1Id, room2Id },
                CustomRoomDataNumberPerRoom = new[] { 2, 1 },
                CustomRoomDataKeys = new[] { "r1k1", "r1k2", "r2k1" },
                CustomRoomDataValues = new[] { "r1v1", "r1v2", "r2v1" },
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.AreEqual(2, result.CustomRoomData.Count);

            var room1Guid = Guid.Parse(room1Id);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(room1Guid));
            var room1Data = result.CustomRoomData[room1Guid];
            Assert.AreEqual(2, room1Data.Count);
            Assert.AreEqual("r1v1", room1Data["r1k1"]);
            Assert.AreEqual("r1v2", room1Data["r1k2"]);

            var room2Guid = Guid.Parse(room2Id);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(room2Guid));
            var room2Data = result.CustomRoomData[room2Guid];
            Assert.AreEqual(1, room2Data.Count);
            Assert.AreEqual("r2v1", room2Data["r2k1"]);
        }

        [Test]
        public void Map_WithMultipleRoomsWithSameKeys_ShouldMapEachRoomCorrectly()
        {
            var room1Id = "11111111-1111-1111-1111-111111111111";
            var room2Id = "22222222-2222-2222-2222-222222222222";
            var room3Id = "33333333-3333-3333-3333-333333333333";

            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = new[] { room1Id, room2Id, room3Id },
                CustomRoomDataNumberPerRoom = new[] { 3, 3, 3 },
                CustomRoomDataKeys = new[]
                {
                    "difficulty", "mapName", "mode", // Room 1
                    "difficulty", "mapName", "mode", // Room 2
                    "difficulty", "mapName", "mode" // Room 3
                },
                CustomRoomDataValues = new[]
                {
                    "easy", "forest", "casual", // Room 1 values
                    "hard", "desert", "ranked", // Room 2 values
                    "medium", "city", "practice" // Room 3 values
                },
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.AreEqual(3, result.CustomRoomData.Count);

            // Verify Room 1
            var room1Guid = Guid.Parse(room1Id);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(room1Guid));
            var room1Data = result.CustomRoomData[room1Guid];
            Assert.AreEqual(3, room1Data.Count);
            Assert.AreEqual("easy", room1Data["difficulty"]);
            Assert.AreEqual("forest", room1Data["mapName"]);
            Assert.AreEqual("casual", room1Data["mode"]);

            // Verify Room 2
            var room2Guid = Guid.Parse(room2Id);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(room2Guid));
            var room2Data = result.CustomRoomData[room2Guid];
            Assert.AreEqual(3, room2Data.Count);
            Assert.AreEqual("hard", room2Data["difficulty"]);
            Assert.AreEqual("desert", room2Data["mapName"]);
            Assert.AreEqual("ranked", room2Data["mode"]);

            // Verify Room 3
            var room3Guid = Guid.Parse(room3Id);
            Assert.IsTrue(result.CustomRoomData.ContainsKey(room3Guid));
            var room3Data = result.CustomRoomData[room3Guid];
            Assert.AreEqual(3, room3Data.Count);
            Assert.AreEqual("medium", room3Data["difficulty"]);
            Assert.AreEqual("city", room3Data["mapName"]);
            Assert.AreEqual("practice", room3Data["mode"]);
        }

        [Test]
        public void Map_WithExternalGameData_ShouldMapCorrectly()
        {
            var externalData = new byte[] { 10, 20, 30, 40, 50 };
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = externalData,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.IsNotNull(result.ExternalGameData);
            CollectionAssert.AreEqual(externalData, result.ExternalGameData);
        }

        [Test]
        public void Map_WithInvalidMatchIdGuid_ShouldThrowFormatException()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = "invalid-guid-format",
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var exception = Assert.Throws<FormatException>(() => message.Map());
            Assert.That(exception.Message, Does.Contain("Invalid MatchId GUID format"));
        }

        [Test]
        public void Map_WithMismatchedCustomMatchmakingDataArrays_ShouldThrowArgumentException()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = new[] { "key1", "key2" },
                CustomMatchmakingDataValues = new[] { "value1" }, // Mismatched length
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var exception = Assert.Throws<ArgumentException>(() => message.Map());
            Assert.That(exception.Message, Does.Contain("CustomMatchmakingDataKeys"));
            Assert.That(exception.Message, Does.Contain("CustomMatchmakingDataValues"));
        }

        [Test]
        public void Map_WithInvalidUserIdGuid_ShouldThrowFormatException()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = "invalid-user-id",
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = Array.Empty<byte>(),
                        MatchmakerData = Array.Empty<float>(),
                        RoomId = ValidRoomId,
                        TeamIndex = 0,
                        Nickname = "TestPlayer",
                        NicknameType = "Common",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    }
                }
            };

            _ = Assert.Throws<FormatException>(() => message.Map());
        }

        [Test]
        public void Map_WithInvalidRoomIdGuid_ShouldThrowFormatException()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = new[] { "invalid-room-guid" },
                CustomRoomDataNumberPerRoom = new[] { 0 },
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            _ = Assert.Throws<FormatException>(() => message.Map());
        }

        [Test]
        public void Map_WithInvalidPlayerRoomIdGuid_ShouldThrowFormatException()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = ValidUserId,
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = Array.Empty<byte>(),
                        MatchmakerData = Array.Empty<float>(),
                        RoomId = "invalid-player-room-id",
                        TeamIndex = 0,
                        Nickname = "TestPlayer",
                        NicknameType = "Common",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    }
                }
            };

            _ = Assert.Throws<FormatException>(() => message.Map());
        }

        [Test]
        public void Map_WithComplexScenario_ShouldMapAllDataCorrectly()
        {
            var room1Id = "11111111-1111-1111-1111-111111111111";
            var room2Id = "22222222-2222-2222-2222-222222222222";
            var user1Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            var user2Id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            var externalData = new byte[] { 1, 2, 3, 4, 5 };

            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "RankedQueue",
                RegionName = "US-WEST",
                RoomGuids = new[] { room1Id, room2Id },
                CustomRoomDataNumberPerRoom = new[] { 1, 2 },
                CustomRoomDataKeys = new[] { "r1k1", "r2k1", "r2k2" },
                CustomRoomDataValues = new[] { "r1v1", "r2v1", "r2v2" },
                CustomMatchmakingDataKeys = new[] { "mmk1", "mmk2", "mmk3" },
                CustomMatchmakingDataValues = new[] { "mmv1", "mmv2", "mmv3" },
                ExternalGameData = externalData,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>
                {
                    new()
                    {
                        UserId = user1Id,
                        IsBot = false,
                        BotDifficulty = 0.0,
                        GameEngineData = new byte[] { 10, 20 },
                        MatchmakerData = new float[] { 100.5f },
                        RoomId = room1Id,
                        TeamIndex = 0,
                        Nickname = "ProPlayer",
                        NicknameType = "Verified",
                        CustomDataKeys = new[] { "skin", "rank" },
                        CustomDataValues = new[] { "gold", "diamond" }
                    },
                    new()
                    {
                        UserId = user2Id,
                        IsBot = true,
                        BotDifficulty = 0.5,
                        GameEngineData = new byte[] { 30 },
                        MatchmakerData = new float[] { 50.0f, 60.0f },
                        RoomId = room2Id,
                        TeamIndex = 1,
                        Nickname = "BotPlayer",
                        NicknameType = "Common",
                        CustomDataKeys = null,
                        CustomDataValues = null
                    }
                }
            };

            var result = message.Map();

            // Verify basic match data
            Assert.AreEqual(Guid.Parse(ValidMatchId), result.MatchId);
            Assert.IsFalse(result.IsReplay);
            Assert.AreEqual("RankedQueue", result.QueueName);
            Assert.AreEqual("US-WEST", result.RegionName);

            // Verify external game data
            CollectionAssert.AreEqual(externalData, result.ExternalGameData);

            // Verify matchmaking data
            Assert.AreEqual(3, result.CustomMatchmakingData.Count);
            Assert.AreEqual("mmv1", result.CustomMatchmakingData["mmk1"]);
            Assert.AreEqual("mmv2", result.CustomMatchmakingData["mmk2"]);
            Assert.AreEqual("mmv3", result.CustomMatchmakingData["mmk3"]);

            // Verify room data
            Assert.AreEqual(2, result.CustomRoomData.Count);
            var room1Guid = Guid.Parse(room1Id);
            Assert.AreEqual(1, result.CustomRoomData[room1Guid].Count);
            Assert.AreEqual("r1v1", result.CustomRoomData[room1Guid]["r1k1"]);
            var room2Guid = Guid.Parse(room2Id);
            Assert.AreEqual(2, result.CustomRoomData[room2Guid].Count);
            Assert.AreEqual("r2v1", result.CustomRoomData[room2Guid]["r2k1"]);
            Assert.AreEqual("r2v2", result.CustomRoomData[room2Guid]["r2k2"]);

            // Verify player data
            Assert.AreEqual(2, result.PlayerInitialDatas.Count);
            var players = result.PlayerInitialDatas.ToList();

            // First player
            Assert.AreEqual(ElympicsPlayer.FromIndex(0), players[0].Player);
            Assert.AreEqual(Guid.Parse(user1Id), players[0].UserId);
            Assert.IsFalse(players[0].IsBot);
            Assert.AreEqual(0.0, players[0].BotDifficulty);
            CollectionAssert.AreEqual(new byte[] { 10, 20 }, players[0].GameEngineData);
            CollectionAssert.AreEqual(new float[] { 100.5f }, players[0].MatchmakerData);
            Assert.AreEqual(Guid.Parse(room1Id), players[0].RoomId);
            Assert.AreEqual(0u, players[0].TeamIndex);
            Assert.AreEqual("ProPlayer", players[0].Nickname);
            Assert.AreEqual(NicknameType.Verified, players[0].NicknameType);
            Assert.AreEqual(2, players[0].CustomData.Count);
            Assert.AreEqual("gold", players[0].CustomData["skin"]);
            Assert.AreEqual("diamond", players[0].CustomData["rank"]);

            // Second player
            Assert.AreEqual(ElympicsPlayer.FromIndex(1), players[1].Player);
            Assert.AreEqual(Guid.Parse(user2Id), players[1].UserId);
            Assert.IsTrue(players[1].IsBot);
            Assert.AreEqual(0.5, players[1].BotDifficulty);
            CollectionAssert.AreEqual(new byte[] { 30 }, players[1].GameEngineData);
            CollectionAssert.AreEqual(new float[] { 50.0f, 60.0f }, players[1].MatchmakerData);
            Assert.AreEqual(Guid.Parse(room2Id), players[1].RoomId);
            Assert.AreEqual(1u, players[1].TeamIndex);
            Assert.AreEqual("BotPlayer", players[1].Nickname);
            Assert.AreEqual(NicknameType.Common, players[1].NicknameType);
            Assert.AreEqual(0, players[1].CustomData.Count);
        }

        [Test]
        public void Map_IsReplayProperty_ShouldAlwaysBeFalse()
        {
            var message = new MatchJoinedMessage
            {
                MatchId = ValidMatchId,
                QueueName = "TestQueue",
                RegionName = "EU",
                RoomGuids = Array.Empty<string>(),
                CustomRoomDataNumberPerRoom = Array.Empty<int>(),
                CustomRoomDataKeys = Array.Empty<string>(),
                CustomRoomDataValues = Array.Empty<string>(),
                CustomMatchmakingDataKeys = Array.Empty<string>(),
                CustomMatchmakingDataValues = Array.Empty<string>(),
                ExternalGameData = null,
                UserInitialMatchData = new List<MatchJoinedMessage.InitialMatchPlayerData>()
            };

            var result = message.Map();

            Assert.IsFalse(result.IsReplay);
        }
    }
}
