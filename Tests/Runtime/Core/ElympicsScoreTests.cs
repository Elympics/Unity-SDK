using System;
using System.Collections;
using System.Collections.Generic;
using Elympics.Core;
using Elympics.Core.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Elympics.Tests.Runtime.Core
{
    [TestFixture]
    [Category("Core")]
    public class ElympicsScoreTests
    {
        private DateTime? _gameStartUtc;

        [SetUp]
        public void SetUp() => _gameStartUtc = null;

        private ElympicsScore CreateScore(int playerCount) =>
            new(playerCount, (_, currentTime) => currentTime - (_gameStartUtc ?? throw new InvalidOperationException("The game has not started yet")));

        private void StartScore(ElympicsScore score, DateTime gameStartUtc)
        {
            _gameStartUtc = gameStartUtc;
            score.Start(gameStartUtc);
        }

        private ElympicsScore CreateStartedScore(int playerCount, IEnumerable<string> metadataKeys = null)
        {
            var score = CreateScore(playerCount);
            score.Enable(metadataKeys: metadataKeys);
            StartScore(score, DateTime.UtcNow);
            return score;
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void RetrievingScoreBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = CreateScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => _ = score[playerIndex]);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void SettingScoreBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = CreateScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => score[playerIndex] = 10f);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void RetrievingMetadataBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = CreateScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => _ = score[playerIndex].Metadata["example-key"]);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void SettingMetadataBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = CreateScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => score[playerIndex].Metadata["example-key"] = 10f);
        }

        [Test]
        public void EnablingMoreThanOneTimeShouldThrow()
        {
            var score = CreateScore(2);
            score.Enable();

            _ = Assert.Throws<InvalidOperationException>(() => score.Enable());
        }

        [Test]
        public void RetrievingScoreShouldReturnCorrectScore()
        {
            var score = CreateScore(2);
            score.Enable();

            score[0] = 2.0f;
            score[0] = 5.0f;

            Assert.That((float)score[0], Is.EqualTo(5.0f));
        }

        [Test]
        public void RetrievingExistingMetadataKeyShouldReturnLastAssignedValue()
        {
            var score = CreateScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            score[0].Metadata["example-key"] = "example-value";
            score[0].Metadata["example-key"] = "example-value2";

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo((JsonValue)"example-value2"));
        }

        [Test]
        public void RetrievingExistingMetadataKeyShouldReturnNullIfThereIsNoValueAssigned()
        {
            var score = CreateScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo(JsonValue.Null));
        }

        [Test]
        public void RetrievingNonExistingMetadataKeyShouldResultInException()
        {
            var score = CreateScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            score[0].Metadata["example-key"] = "example-value";

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = score[0].Metadata["unknown-key"]);
        }

        [Test]
        public void AssigningToNonExistingMetadataKeyShouldResultInException()
        {
            var score = CreateScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => score[0].Metadata["unknown-key"] = "example-value");
        }

        [Test]
        public void SettingScoreOrMetadataShouldNotCauseEventToBeEmitted()
        {
            var score = CreateScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });
            (int PlayerIndex, float Score, string Metadata)? updatedScore = null;
            score.PlayerScoreUpdated += (p, s, m, _, _) => updatedScore = (p, s, m);

            score[1] = 5.0f;
            score[1].Metadata["example-key"] = "example-value";

            Assert.That(updatedScore.HasValue, Is.False);
        }

        [Test]
        public void SubmittingChangedScoreShouldCauseEventToBeEmitted()
        {
            var score = CreateStartedScore(2);
            (int PlayerIndex, float Score, string Metadata)? updatedScore = null;
            score.PlayerScoreUpdated += (p, s, m, _, _) => updatedScore = (p, s, m);

            score[1] = 5.0f;
            score.Submit();

            Assert.That(updatedScore.HasValue, Is.True);
            Assert.That(updatedScore!.Value.PlayerIndex, Is.EqualTo(1));
            Assert.That(updatedScore!.Value.Score, Is.EqualTo(5.0f));
            Assert.That(updatedScore!.Value.Metadata, Is.EqualTo("{}"));
        }

        [Test]
        public void SubmittingChangedMetadataShouldCauseEventToBeEmitted()
        {
            var score = CreateStartedScore(2, metadataKeys: new[] { "example-key" });
            (int PlayerIndex, float Score, string Metadata)? updatedScore = null;
            score.PlayerScoreUpdated += (p, s, m, _, _) => updatedScore = (p, s, m);

            score[1].Metadata["example-key"] = "example-value";
            score.Submit();

            Assert.That(updatedScore.HasValue, Is.True);
            Assert.That(updatedScore!.Value.PlayerIndex, Is.EqualTo(1));
            Assert.That(updatedScore!.Value.Score, Is.EqualTo(0.0f));
            Assert.That(updatedScore!.Value.Metadata, Is.EqualTo("{\"example-key\":\"example-value\"}"));
        }

        [Test]
        public void SubmittingWithoutChangesShouldNotCauseEventToBeEmitted()
        {
            var score = CreateStartedScore(2, metadataKeys: new[] { "example-key" });
            (int PlayerIndex, float Score, string Metadata)? updatedScore = null;
            score[1] = 5.0f;
            score[1].Metadata["example-key"] = "example-value";
            score.Submit();
            score.PlayerScoreUpdated += (p, s, m, _, _) => updatedScore = (p, s, m);

            score.Submit();

            Assert.That(updatedScore.HasValue, Is.False);
        }

        [Test]
        public void RetrievingStartingScoreBeforeSettingFirstScoreShouldReturnNull()
        {
            var score = CreateScore(1);
            score.Enable();

            Assert.That(score.GetStartingScoreUtcTime(0), Is.Null);
        }

        [Test]
        public void UnsubmittedScoreShouldNotSetStartingTime()
        {
            var score = CreateScore(1);
            score.Enable();

            score[0] = 1f;
            var startingTime = score.GetStartingScoreUtcTime(0);

            Assert.That(startingTime, Is.Null);
        }

        [UnityTest]
        public IEnumerator StartingScoreTimeShouldNotChange()
        {
            var score = CreateStartedScore(1);

            score[0] = 1f;
            score.Submit();
            var firstTime = score.GetStartingScoreUtcTime(0);
            yield return null;
            score[0] = 2f;
            score.Submit();
            var secondTime = score.GetStartingScoreUtcTime(0);

            Assert.That(firstTime, Is.Not.Null);
            Assert.That(firstTime, Is.EqualTo(secondTime));
        }

        [UnityTest]
        public IEnumerator DefaultGameplayTimeShouldBeCalculatedAsDiffToStartingTime()
        {
            var score = CreateStartedScore(1);

            score[0] = 1f;
            score.Submit();
            var firstTime = score.GetCurrentScoreUtcTime(0);
            yield return null;
            score[0] = 2f;
            score.Submit();
            var secondTime = score.GetCurrentScoreUtcTime(0);

            Assert.That(firstTime, Is.LessThanOrEqualTo(secondTime));
            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(secondTime - score.GetStartingScoreUtcTime(0)));
        }

        [UnityTest]
        public IEnumerator UnsubmittedScoreShouldNotChangeCurrentTime()
        {
            var score = CreateStartedScore(1);

            score[0] = 1f;
            score.Submit();
            var firstTime = score.GetCurrentScoreUtcTime(0);
            yield return null;
            score[0] = 2f;
            var secondTime = score.GetCurrentScoreUtcTime(0);

            Assert.That(firstTime, Is.EqualTo(secondTime));
        }

        [Test]
        public void ProvidedDelegateShouldBeUsedForCalculatingGameplayTime()
        {
            var score = CreateScore(1);
            var expectedGameplayTime = TimeSpan.FromSeconds(10);
            score.Enable((_, _) => expectedGameplayTime);
            StartScore(score, DateTime.UtcNow);

            score[0] = 100f;
            score.Submit();

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
        }

        [Test]
        public void SubmittingBeforeStartShouldNotCauseEventToBeEmitted()
        {
            var score = CreateScore(2);
            score.Enable();
            var emitted = false;
            score.PlayerScoreUpdated += (_, _, _, _, _) => emitted = true;

            score[1] = 5.0f;
            score.Submit(true);
            score.SubmitIfEnabled();

            Assert.That(emitted, Is.False);
        }

        [Test]
        public void StartShouldSubmitAllPlayersWithZeroGameplayTime()
        {
            var score = CreateScore(2);
            score.Enable();
            score[1] = 5.0f;
            var updates = new List<(int PlayerIndex, float Score, TimeSpan GameplayTime)>();
            score.PlayerScoreUpdated += (p, s, _, _, t) => updates.Add((p, s, t));

            StartScore(score, DateTime.UtcNow);

            Assert.That(updates, Is.EqualTo(new[] { (0, 0f, TimeSpan.Zero), (1, 5f, TimeSpan.Zero) }));
        }

        [Test]
        public void StartShouldEnableScoreIfNotEnabled()
        {
            var score = CreateScore(1);
            var start = DateTime.UtcNow;

            StartScore(score, start);

            Assert.That((float)score[0], Is.EqualTo(0f));
            Assert.That(score.GetStartingScoreUtcTime(0), Is.EqualTo(start));
        }

        [Test]
        public void EnablingOnceAfterAutomaticEnableShouldReplaceSettings()
        {
            var score = CreateScore(1);
            StartScore(score, DateTime.UtcNow);
            var expectedGameplayTime = TimeSpan.FromSeconds(10);

            score.Enable((_, _) => expectedGameplayTime);
            score[0] = 1f;
            score.Submit();

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
            _ = Assert.Throws<InvalidOperationException>(() => score.Enable());
        }

        [Test]
        public void DefaultGameplayTimeShouldCountFromStart()
        {
            var score = CreateScore(1);
            score.Enable();
            var start = DateTime.UtcNow - TimeSpan.FromSeconds(3);
            StartScore(score, start);

            score[0] = 1f;
            score.Submit();

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(score.GetCurrentScoreUtcTime(0) - start));
        }
    }
}
