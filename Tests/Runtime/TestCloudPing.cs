using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Elympics.Tests
{
    [TestFixture]
    public class TestCloudPing
    {
        private PingResultResultFactoryMock _pingResultFactory;
        [OneTimeSetUp]
        public void SutSetup()
        {
            _pingResultFactory = new PingResultResultFactoryMock();
            ElympicsCloudPing.PingResultFactory = _pingResultFactory;
        }

        [UnityTest]
        public IEnumerator TestSameRegionCopiedMultipleTimes([ValueSource(typeof(ElympicsRegions), nameof(ElympicsRegions.AllAvailableRegions))] string region) => UniTask.ToCoroutine(async () =>
        {
            var result = await ElympicsCloudPing.ChooseClosestRegion(new[]
            {
                region, region
            });
            await TestContext.Out.WriteLineAsync($"Region: {result.Region}, latency: {result.LatencyMs} ms");
            Assert.AreEqual(region, result.Region);
        });

        [UnityTest]
        public IEnumerator TestSingleRegion([ValueSource(typeof(ElympicsRegions), nameof(ElympicsRegions.AllAvailableRegions))] string region) => UniTask.ToCoroutine(async () =>
        {
            IList<string> test = new List<string>
            {
                region
            };
            var result = await ElympicsCloudPing.ChooseClosestRegion(test);
            await TestContext.Out.WriteLineAsync($"Region: {result.Region}, latency: {result.LatencyMs} ms");
            Assert.AreEqual(region, result.Region);
        });

        [UnityTest]
        public IEnumerator TestAllRegions() => UniTask.ToCoroutine(async () =>
        {
            var result = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
            await TestContext.Out.WriteLineAsync($"Region: {result.Region}, latency: {result.LatencyMs} ms");
            Assert.Contains(result.Region, ElympicsRegions.AllAvailableRegions);
        });

        [UnityTest]
        public IEnumerator TestLatencyDataForAllRegions() => UniTask.ToCoroutine(async () =>
        {
            var data = await ElympicsCloudPing.GetLatencyDataForRegions(ElympicsRegions.AllAvailableRegions);
            Assert.AreEqual(ElympicsRegions.AllAvailableRegions.Count, data.Count);
            foreach (var (region, latency) in data)
            {
                await TestContext.Out.WriteLineAsync($"Region: {region}, latency: {latency}");
                Assert.Contains(region, ElympicsRegions.AllAvailableRegions);
            }
        });

        private static List<string> testRegions = new(ElympicsRegions.AllAvailableRegions);

        [UnityTest]
        public IEnumerator TestIfInvalidRegionsWontBeIncludedInResult([ValueSource(nameof(testRegions))] string invalidRegion) => UniTask.ToCoroutine(async () =>
        {
            _pingResultFactory.SetInvalidRegion(invalidRegion);
            var data = await ElympicsCloudPing.GetLatencyDataForRegions(ElympicsRegions.AllAvailableRegions);
        });

        [UnityTest]
        public IEnumerator TestIfClosestRegionWillBeSelectedAsLowestPing([ValueSource(nameof(testRegions))] string closestRegion) => UniTask.ToCoroutine(async () =>
        {
            _pingResultFactory.SetClosestRegion(closestRegion);
            var data = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
            Assert.AreEqual(closestRegion, data.Region);
        });

        [UnityTest]
        public IEnumerator TestZeroRegions() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.ChooseClosestRegion(Array.Empty<string>());
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestNullRegions() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.ChooseClosestRegion(null);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentNullException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestNonExistingRegion() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.ChooseClosestRegion(new[]
                {
                    "non-existing-region"
                });
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestLatencyDataZeroRegions() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.GetLatencyDataForRegions(Array.Empty<string>());
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestLatencyDataNullRegions() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.GetLatencyDataForRegions(null);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentNullException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestLatencyDataNonExistingRegion() => UniTask.ToCoroutine(async () =>
        {
            Exception exc = null;
            try
            {
                _ = await ElympicsCloudPing.GetLatencyDataForRegions(new[]
                {
                    "non-existing-region"
                });
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
        });

        [TearDown]
        public void Reset()
        {
            _pingResultFactory.Reset();
        }
    }
}
