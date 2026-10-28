using System;
using System.Collections;
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
        [TestCase(0)]
        [TestCase(3)]
        public void RetrievingMetadataBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = new ElympicsScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => _ = score[playerIndex].Metadata["example-key"]);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        public void SettingMetadataBeforeEnablingShouldResultInException(int playerIndex)
        {
            var score = new ElympicsScore(2);
            _ = Assert.Throws<InvalidOperationException>(() => score[playerIndex].Metadata["example-key"] = 10f);
        }

        [Test]
        public void EnablingMoreThanOneTimeShouldThrow()
        {
            var score = new ElympicsScore(2);
            score.Enable();

            _ = Assert.Throws<InvalidOperationException>(() => score.Enable());
        }

        [Test]
        public void RetrievingScoreShouldReturnCorrectScore()
        {
            var score = new ElympicsScore(2);
            score.Enable();

            score[0] = 2.0f;
            score[0] = 5.0f;

            Assert.That((float)score[0], Is.EqualTo(5.0f));
        }

        [Test]
        public void RetrievingExistingMetadataKeyShouldReturnLastAssignedValue()
        {
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            score[0].Metadata["example-key"] = "example-value";
            score[0].Metadata["example-key"] = "example-value2";

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo((JsonValue)"example-value2"));
        }

        [Test]
        public void RetrievingExistingMetadataKeyShouldReturnNullIfThereIsNoValueAssigned()
        {
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo(JsonValue.Null));
        }

        [Test]
        public void RetrievingNonExistingMetadataKeyShouldResultInException()
        {
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            score[0].Metadata["example-key"] = "example-value";

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = score[0].Metadata["unknown-key"]);
        }

        [Test]
        public void AssigningToNonExistingMetadataKeyShouldResultInException()
        {
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => score[0].Metadata["unknown-key"] = "example-value");
        }

        [Test]
        public void SettingScoreOrMetadataShouldNotCauseEventToBeEmitted()
        {
            var score = new ElympicsScore(2);
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
            var score = new ElympicsScore(2);
            score.Enable();
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
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });
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
            var score = new ElympicsScore(2);
            score.Enable(metadataKeys: new[] { "example-key" });
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
            var score = new ElympicsScore(1);
            score.Enable();

            Assert.That(score.GetStartingScoreUtcTime(0), Is.Null);
        }

        [Test]
        public void UnsubmittedScoreShouldNotSetStartingTime()
        {
            var score = new ElympicsScore(1);
            score.Enable();

            score[0] = 1f;
            var startingTime = score.GetStartingScoreUtcTime(0);

            Assert.That(startingTime, Is.Null);
        }

        [UnityTest]
        public IEnumerator StartingScoreTimeShouldNotChange()
        {
            var score = new ElympicsScore(1);
            score.Enable();

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
            var score = new ElympicsScore(1);
            score.Enable();

            score[0] = 1f;
            score.Submit();
            var firstTime = score.GetCurrentScoreUtcTime(0);
            yield return null;
            score[0] = 2f;
            score.Submit();
            var secondTime = score.GetCurrentScoreUtcTime(0);

            Assert.That(firstTime, Is.LessThanOrEqualTo(secondTime));
            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(secondTime - firstTime));
        }

        [UnityTest]
        public IEnumerator UnsubmittedScoreShouldNotChangeCurrentTime()
        {
            var score = new ElympicsScore(1);
            score.Enable();

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
            var score = new ElympicsScore(1);
            var expectedGameplayTime = TimeSpan.FromSeconds(10);
            score.Enable((_, _) => expectedGameplayTime);

            score[0] = 100f;
            score.Submit();

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
        }
    }
}
