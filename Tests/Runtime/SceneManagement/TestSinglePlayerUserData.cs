using System;
using System.Linq;
using Elympics.Models.Matchmaking;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [Category("SinglePlayer")]
    public class TestSinglePlayerUserData
    {
        private static readonly Guid RealPlayerId = new("11111111-2222-3333-4444-555555555555");

        private ElympicsGameConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _config.maxPlayers = 4;
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private static MatchmakingFinishedData MatchData(params Guid[] players) =>
            new(Guid.NewGuid(), "", "queue", "region", new byte[] { 1, 2 }, new[] { 3f }, "", "", players);

        private void AddTestPlayer(bool isBot, double difficulty = 0) =>
            _config.TestPlayers.Add(new ElympicsGameConfig.InitialUserData { isBot = isBot, botDifficulty = difficulty });

        [Test]
        public void NoTestPlayersGivesOneRealPlayer()
        {
            var users = SinglePlayerGameInitializer.CreateUserData(MatchData(RealPlayerId), _config);

            Assert.That(users, Has.Count.EqualTo(1));
            Assert.That(users[0].UserId, Is.EqualTo(RealPlayerId));
            Assert.That(users[0].IsBot, Is.False);
            Assert.That(users[0].GameEngineData, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(users[0].MatchmakerData, Is.EqualTo(new[] { 3f }));
        }

        [Test]
        public void BotsFromTestPlayersFillRemainingSlots()
        {
            AddTestPlayer(false);
            AddTestPlayer(true, 0.5);
            AddTestPlayer(true, 0.75);

            var users = SinglePlayerGameInitializer.CreateUserData(MatchData(RealPlayerId), _config);

            Assert.That(users, Has.Count.EqualTo(3));
            Assert.That(users[0].UserId, Is.EqualTo(RealPlayerId));
            Assert.That(users[0].IsBot, Is.False);
            Assert.That(users.Skip(1).Select(u => u.IsBot), Is.All.True);
            Assert.That(users.Skip(1).Select(u => u.BotDifficulty), Is.EqualTo(new[] { 0.5, 0.75 }));
            Assert.That(users.Select(u => u.UserId), Is.Unique);
        }

        [Test]
        public void NonBotTestPlayersPastFirstAreIgnored()
        {
            AddTestPlayer(false);
            AddTestPlayer(false);
            AddTestPlayer(true);

            var users = SinglePlayerGameInitializer.CreateUserData(MatchData(RealPlayerId), _config);

            Assert.That(users, Has.Count.EqualTo(2));
            Assert.That(users.Count(u => !u.IsBot), Is.EqualTo(1));
        }

        [Test]
        public void PlayerCountIsClampedToMaxPlayers()
        {
            _config.maxPlayers = 2;
            AddTestPlayer(true);
            AddTestPlayer(true);
            AddTestPlayer(true);

            var users = SinglePlayerGameInitializer.CreateUserData(MatchData(RealPlayerId), _config);

            Assert.That(users, Has.Count.EqualTo(2));
            Assert.That(users[0].IsBot, Is.False);
        }

        [Test]
        public void EmptyMatchedPlayersFallsBackToEmptyGuid()
        {
            var users = SinglePlayerGameInitializer.CreateUserData(MatchData(), _config);

            Assert.That(users, Has.Count.EqualTo(1));
            Assert.That(users[0].UserId, Is.EqualTo(Guid.Empty));
        }
    }
}
