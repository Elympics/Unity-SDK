using System;
using Elympics.Core;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Core
{
    [TestFixture]
    [Category("Core")]
    public class ElympicsScoreTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void RetrievingScoreBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = new ElympicsScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => _ = score[playerIndex]);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void SettingScoreBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = new ElympicsScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => score[playerIndex] = 10f);
        }

        [Test]
        public void RetrievingScoreShouldReturnCorrectScore()
        {
            var score = new ElympicsScore(2);
            score.Enable();

            score[0] = 2.0f;
            score[0] = 5.0f;

            Assert.That(score[0], Is.EqualTo(5.0f));
        }

        [Test]
        public void SettingScoreShouldCauseEventToBeEmitted()
        {
            var score = new ElympicsScore(2);
            score.Enable();
            (int PlayerIndex, float Score)? updatedScore = null;
            score.PlayerScoreUpdated += (p, s, _, _) => updatedScore = (p, s);

            score[1] = 5.0f;

            Assert.That(updatedScore.HasValue, Is.True);
            Assert.That(updatedScore!.Value.PlayerIndex, Is.EqualTo(1));
            Assert.That(updatedScore!.Value.Score, Is.EqualTo(5.0f));
        }

        [Test]
        public void RetrievingStartingScoreBeforeSettingFirstScoreShouldReturnNull()
        {
            var score = new ElympicsScore(1);
            score.Enable();

            Assert.That(score.GetStartingScoreUtcTime(0), Is.Null);
        }

        [Test]
        public void StartingScoreShouldNotChange()
        {
            var score = new ElympicsScore(1);
            score.Enable();

            score[0] = 1f;
            var firstTime = score.GetStartingScoreUtcTime(0);
            score[0] = 2f;
            var secondTime = score.GetStartingScoreUtcTime(0);

            Assert.That(firstTime, Is.Not.Null);
            Assert.That(firstTime, Is.EqualTo(secondTime));
        }

        [Test]
        public void DefaultGameplayTimeShouldBeCalculatedAsDiffToStartingTime()
        {
            var score = new ElympicsScore(1);
            score.Enable();

            score[0] = 1f;
            var firstTime = score.GetCurrentScoreUtcTime(0);
            score[0] = 2f;
            var secondTime = score.GetCurrentScoreUtcTime(0);

            Assert.That(firstTime, Is.LessThanOrEqualTo(secondTime));
            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(secondTime - firstTime));
        }

        [Test]
        public void ProvidedDelegateShouldBeUsedForCalculatingGameplayTime()
        {
            var score = new ElympicsScore(1);
            var expectedGameplayTime = TimeSpan.FromSeconds(10);
            score.Enable(_ => expectedGameplayTime);

            score[0] = 100f;

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
        }
    }
}
