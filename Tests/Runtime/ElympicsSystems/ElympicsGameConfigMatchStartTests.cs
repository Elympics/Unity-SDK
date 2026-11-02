using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("Core")]
    public class ElympicsGameConfigMatchStartTests
    {
        private ElympicsGameConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<ElympicsGameConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // Defaults: input lag 2 + snapshot period 1 + prediction limit 25 = 28 ticks, plus the margin of 2 = 30 ticks.
        [TestCase(1f, 30, 30L, Description = "Seconds and the prediction limit give the same ticks.")]
        [TestCase(5f, 30, 150L, Description = "The configured delay is longer than the minimum.")]
        [TestCase(0f, 30, 30L, Description = "No delay is raised to the minimum.")]
        [TestCase(0.5f, 30, 30L, Description = "A too short delay is raised to the minimum.")]
        [TestCase(1.01f, 30, 31L, Description = "Seconds are rounded up to whole ticks.")]
        [TestCase(2f, 60, 120L, Description = "Ticks per second scale the delay.")]
        public void MatchStartDelayTicksIsTheLongerOfConfiguredDelayAndPredictionLimit(float seconds, int ticksPerSecond, long expectedTicks)
        {
            SetField("matchStartDelaySeconds", seconds);
            SetField("ticksPerSecond", ticksPerSecond);

            Assert.That(_config.TotalPredictionLimitInTicks + ElympicsGameConfig.MatchStartDelayMarginTicks, Is.EqualTo(30));
            Assert.That(_config.MatchStartDelayTicks, Is.EqualTo(expectedTicks));
            Assert.That(_config.MatchStartDelaySeconds, Is.EqualTo(expectedTicks * _config.TickDuration).Within(1e-5f));
        }

        [Test]
        public void MatchStartDelayTicksFollowsThePredictionLimit()
        {
            SetField("matchStartDelaySeconds", 0f);
            SetField("predictionLimitInTicks", 50);

            Assert.That(_config.MatchStartDelayTicks, Is.EqualTo(_config.TotalPredictionLimitInTicks + ElympicsGameConfig.MatchStartDelayMarginTicks));
            Assert.That(_config.MatchStartDelayTicks, Is.EqualTo(55L));
        }

        private void SetField(string name, object value) =>
            typeof(ElympicsGameConfig).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_config, value);
    }
}
