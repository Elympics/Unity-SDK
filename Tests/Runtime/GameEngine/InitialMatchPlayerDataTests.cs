using System;
using System.Collections.Generic;
using GameEngineCore;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests.Runtime.ElympicsSystems
{
    [TestFixture]
    [Category("GameEngine")]
    public class InitialMatchPlayerDataTests
    {
        [Test]
        [TestCase(uint.MinValue)]
        [TestCase(uint.MaxValue)]
        public void SeedShouldBeAvailableIfCorrectUintValueIsPassed(uint expectedSeedValue)
        {
            var seedValue = expectedSeedValue.ToString();
            var initialGameDatas = CreateInitialDataWithSeed(seedValue);

            Assert.That(initialGameDatas.Seed, Is.EqualTo(expectedSeedValue));
        }

        [Test]
        public void MissingSeedFieldShouldResultInNullSeed()
        {
            var initialGameDatas = CreateInitialDataWithSeed(null);

            Assert.That(initialGameDatas.Seed, Is.Null);
        }

        [Test]
        [TestCase("")]
        [TestCase(-1)]
        [TestCase(ulong.MaxValue)]
        [TestCase(3.14)]
        public void NonParsableSeedFieldShouldResultInNullSeed(object seed)
        {
            var seedValue = seed is string stringSeed ? stringSeed : seed.ToString();
            var initialGameDatas = CreateInitialDataWithSeed(seedValue);

            Assert.That(initialGameDatas.Seed, Is.Null);
        }

        private static InitialMatchPlayerDatasGuid CreateInitialDataWithSeed(string? seedValue) => new(new InitialMatchData
        {
            CustomMatchmakingData = seedValue != null ? new Dictionary<string, string>
            {
                [TournamentConst.SeedKey] = seedValue,
            } : new Dictionary<string, string>(),
            UserData = new List<InitialMatchUserData>(),
        }, new Dictionary<Guid, ElympicsPlayer>(), false);
    }
}
