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

        private static KeyValuePair<string, JsonValue> CreateInitial(string key, JsonValue value) => new(key, value);

        private string CaptureFirstMetadata(ElympicsScore score, int playerIndex = 0)
        {
            string metadata = null;
            score.PlayerScoreUpdated += (p, _, m, _, _) =>
            {
                if (p == playerIndex && metadata == null)
                    metadata = m;
            };
            StartScore(score, DateTime.UtcNow);
            return metadata;
        }

        private ElympicsScore CreateStartedScore(int playerCount, params KeyValuePair<string, JsonValue>[] initialMetadata)
        {
            var score = CreateScore(playerCount);
            score.Enable(null, initialMetadata);
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
            score.Enable(null, CreateInitial("example-key", "initial"));

            score[0].Metadata["example-key"] = "example-value";
            score[0].Metadata["example-key"] = "example-value2";

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo((JsonValue)"example-value2"));
        }

        [Test]
        public void RetrievingExistingMetadataKeyShouldReturnInitialValueIfThereIsNoValueAssigned()
        {
            var score = CreateScore(2);
            score.Enable(null, CreateInitial("example-key", "initial"));

            Assert.That(score[0].Metadata["example-key"], Is.EqualTo((JsonValue)"initial"));
        }

        [Test]
        public void RetrievingNonExistingMetadataKeyShouldResultInException()
        {
            var score = CreateScore(2);
            score.Enable(null, CreateInitial("example-key", "initial"));

            score[0].Metadata["example-key"] = "example-value";

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = score[0].Metadata["unknown-key"]);
        }

        [Test]
        public void AssigningToNonExistingMetadataKeyShouldResultInException()
        {
            var score = CreateScore(2);
            score.Enable(null, CreateInitial("example-key", "initial"));

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => score[0].Metadata["unknown-key"] = "example-value");
        }

        [Test]
        public void SettingScoreOrMetadataShouldNotCauseEventToBeEmitted()
        {
            var score = CreateScore(2);
            score.Enable(null, CreateInitial("example-key", "initial"));
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
            var score = CreateStartedScore(2, CreateInitial("example-key", "initial"));
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
            var score = CreateStartedScore(2, CreateInitial("example-key", "initial"));
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

        private static readonly string[] ExpectedInitialProperties = { "\"pegsBroken\":0", "\"bonusPegsBroken\":0", "\"name\":\"x\"", "\"alive\":true" };

        /// <summary>Splits a flat JSON object into its properties; key order isn't kept by the score.</summary>
        private static string[] SplitProperties(string json) =>
            json == "{}" ? Array.Empty<string>() : json[1..^1].Split(',');

        [Test]
        public void EnablingWithParamsInitialValuesShouldPutThemInFirstSubmission()
        {
            var score = CreateScore(2);
            score.Enable(null,
                CreateInitial("pegsBroken", 0),
                CreateInitial("bonusPegsBroken", 0),
                CreateInitial("name", "x"),
                CreateInitial("alive", true));

            Assert.That(SplitProperties(CaptureFirstMetadata(score)), Is.EquivalentTo(ExpectedInitialProperties));
        }

        [Test]
        public void EnablingWithTupleInitialValuesShouldPutThemInFirstSubmission()
        {
            var score = CreateScore(2);
            score.Enable(null, new List<(string Key, JsonValue Initial)>
            {
                ("pegsBroken", 0), ("bonusPegsBroken", 0), ("name", "x"), ("alive", true),
            });

            Assert.That(SplitProperties(CaptureFirstMetadata(score, 1)), Is.EquivalentTo(ExpectedInitialProperties));
        }

        [Test]
        public void EnablingWithKeyValuePairInitialValuesShouldPutThemInFirstSubmission()
        {
            var score = CreateScore(2);
            score.Enable(null, new List<KeyValuePair<string, JsonValue>>
            {
                CreateInitial("pegsBroken", 0), CreateInitial("bonusPegsBroken", 0), CreateInitial("name", "x"), CreateInitial("alive", true),
            });

            Assert.That(SplitProperties(CaptureFirstMetadata(score)), Is.EquivalentTo(ExpectedInitialProperties));
        }

        [Test]
        public void EnablingWithGameplayTimeAndInitialValuesShouldUseBoth()
        {
            var score = CreateScore(1);
            var expectedGameplayTime = TimeSpan.FromSeconds(10);
            score.Enable((_, _) => expectedGameplayTime, CreateInitial("a", 1));
            StartScore(score, DateTime.UtcNow);

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
            Assert.That(score[0].Metadata["a"], Is.EqualTo((JsonValue)1));
        }

        [Test]
        public void EnablingWithoutArgumentsOrWithOnlyGameplayTimeShouldProduceEmptyMetadata()
        {
            var first = CreateScore(1);
            first.Enable();
            var second = CreateScore(1);
            second.Enable((_, _) => TimeSpan.Zero);

            Assert.That(CaptureFirstMetadata(first), Is.EqualTo("{}"));
            Assert.That(CaptureFirstMetadata(second), Is.EqualTo("{}"));
        }

        [Test]
        public void InitialValuesShouldBeKeptAfterChanges()
        {
            var score = CreateStartedScore(1, CreateInitial("z", 0), CreateInitial("a", 0), CreateInitial("m", 0));
            string metadata = null;
            score.PlayerScoreUpdated += (_, _, m, _, _) => metadata = m;

            score[0].Metadata["m"] = 5;
            score.Submit();

            Assert.That(SplitProperties(metadata), Is.EquivalentTo(new[] { "\"z\":0", "\"a\":0", "\"m\":5" }));
        }

        [Test]
        public void InitialValuesShouldBeIndependentPerPlayer()
        {
            var score = CreateScore(2);
            score.Enable(null, CreateInitial("a", 1));

            score[0].Metadata["a"] = 7;

            Assert.That(score[1].Metadata["a"], Is.EqualTo((JsonValue)1));
        }

        [Test]
        public void EnablingWithNullInitialValueShouldBeAccepted()
        {
            var score = CreateScore(1);

            score.Enable(null, CreateInitial("a", JsonValue.Null));

            Assert.That(score.IsEnabled, Is.True);
            Assert.That(score[0].Metadata["a"], Is.EqualTo(JsonValue.Null));
            Assert.That(CaptureFirstMetadata(score), Is.EqualTo("{\"a\":null}"));
        }

        [Test]
        public void EnablingWithNonFiniteInitialValueShouldThrow()
        {
            var score = CreateScore(1);

            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, CreateInitial("a", double.NaN)));
            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, new List<(string Key, JsonValue Initial)> { ("a", double.PositiveInfinity) }));
            Assert.That(score.IsEnabled, Is.False);
        }

        [Test]
        public void SettingMetadataToNullShouldBeAccepted()
        {
            var score = CreateStartedScore(1, CreateInitial("a", 1));

            score[0].Metadata["a"] = JsonValue.Null;

            Assert.That(score[0].Metadata["a"], Is.EqualTo(JsonValue.Null));
        }

        [Test]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void SettingNonFiniteMetadataShouldThrowAndKeepValue(double value)
        {
            var score = CreateStartedScore(1, CreateInitial("a", 1));

            _ = Assert.Throws<ArgumentException>(() => score[0].Metadata["a"] = value);

            Assert.That(score[0].Metadata["a"], Is.EqualTo((JsonValue)1));
        }

        [Test]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SettingNonFiniteScoreShouldThrow(float value)
        {
            var score = CreateStartedScore(1);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => score.SetScore(0, value));
        }

        [Test]
        public void SubmittingFinalScoresShouldSendAllPlayersEvenWithoutChanges()
        {
            var score = CreateStartedScore(2);
            var updated = new List<int>();
            score.PlayerScoreUpdated += (p, _, _, _, _) => updated.Add(p);

            score.SubmitIfEnabled(true);

            Assert.That(updated, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void SubmittingFinalScoresShouldDoNothingWhenNotEnabledOrNotStarted()
        {
            var notEnabled = CreateScore(1);
            var notStarted = CreateScore(1);
            notStarted.Enable();
            var count = 0;
            notEnabled.PlayerScoreUpdated += (_, _, _, _, _) => count++;
            notStarted.PlayerScoreUpdated += (_, _, _, _, _) => count++;

            notEnabled.SubmitIfEnabled(true);
            notStarted.SubmitIfEnabled(true);

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void EnablingWithDuplicateKeyShouldThrow()
        {
            var score = CreateScore(1);

            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, CreateInitial("a", 1), CreateInitial("a", 2)));
            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, new List<(string Key, JsonValue Initial)> { ("a", 1), ("a", 2) }));
            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, new List<KeyValuePair<string, JsonValue>> { CreateInitial("a", 1), CreateInitial("a", 2) }));
            Assert.That(score.IsEnabled, Is.False);
        }

        [Test]
        public void EnablingWithNullInitialValuesCollectionShouldThrow()
        {
            var score = CreateScore(1);

            _ = Assert.Throws<ArgumentNullException>(() => score.Enable(null, (IEnumerable<(string Key, JsonValue Initial)>)null));
            _ = Assert.Throws<ArgumentNullException>(() => score.Enable(null, (IEnumerable<KeyValuePair<string, JsonValue>>)null));
        }

        [Test]
        public void EnablingWithInitialValuesAfterAutomaticEnableShouldReplaceThem()
        {
            var score = CreateScore(1);
            StartScore(score, DateTime.UtcNow);

            score.Enable(null, CreateInitial("a", 3));

            Assert.That(score[0].Metadata["a"], Is.EqualTo((JsonValue)3));
        }

#pragma warning disable CS0618 // The obsolete overload must keep working
        [Test]
        public void EnablingWithObsoleteMetadataKeysShouldStartWithNullsAndSkipDuplicates()
        {
            var score = CreateScore(1);
            score.Enable(null, new[] { "b", "a", "b" });

            Assert.That(score[0].Metadata["a"], Is.EqualTo(JsonValue.Null));
            Assert.That(SplitProperties(CaptureFirstMetadata(score)), Is.EquivalentTo(new[] { "\"b\":null", "\"a\":null" }));
        }

        [Test]
        public void EnablingWithObsoleteMetadataKeysShouldAcceptGameplayTimeAndNullKeys()
        {
            var score = CreateScore(1);
            var expectedGameplayTime = TimeSpan.FromSeconds(4);
            score.Enable((_, _) => expectedGameplayTime, (IEnumerable<string>)null);
            StartScore(score, DateTime.UtcNow);

            Assert.That(score.GetCurrentScoreGameplayTime(0), Is.EqualTo(expectedGameplayTime));
        }

        [Test]
        public void EnablingWithObsoleteNamedMetadataKeysArgumentShouldStillWork()
        {
            var score = CreateScore(1);
            score.Enable(metadataKeys: new[] { "a" });

            Assert.That(CaptureFirstMetadata(score), Is.EqualTo("{\"a\":null}"));
        }

        [Test]
        public void EnablingWithObsoleteMetadataKeysShouldRejectNullKeyAndStayEnableable()
        {
            var score = CreateScore(1);
            _ = Assert.Throws<ArgumentException>(() => score.Enable(null, new[] { "a", null }));

            score.Enable(metadataKeys: new[] { "a" });
            Assert.That(CaptureFirstMetadata(score), Is.EqualTo("{\"a\":null}"));
        }
#pragma warning restore CS0618

        [Test]
        public void IntegralNumbersInMetadataShouldBeSerializedWithoutTrailingZeroTrimming()
        {
            var score = CreateScore(1);
            score.Enable(null, CreateInitial("zero", 0), CreateInitial("ten", 10), CreateInitial("hundred", 100.0));

            Assert.That(SplitProperties(CaptureFirstMetadata(score)), Is.EquivalentTo(new[] { "\"zero\":0", "\"ten\":10", "\"hundred\":100" }));
        }
    }
}
