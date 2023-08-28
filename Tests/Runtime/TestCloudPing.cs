using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Elympics.Tests
{
    [TestFixture]
    public class TestCloudPing
    {
        private const string HealthQuery = "health";
        private const int WebRequestTimeOutSec = 5;
        private static bool hasInternetConnection;
        private static bool hasBeenSetup;
        private static string closestRegion;

        [UnitySetUp]
        public IEnumerator CheckInternetConnectivityAndFindClosestRegion()
        {
            if (hasBeenSetup)
            {
                if (!hasInternetConnection)
                    Assert.Ignore("No internet Connection.");

                yield break;
            }

            var elympicsConfig = ScriptableObject.CreateInstance<ElympicsConfig>();
            var url = elympicsConfig.ElympicsLobbyEndpoint;
            var builder = new UriBuilder(url);
            builder.Path = $"{builder.Path.TrimEnd('/')}/{HealthQuery}";

            var waitingForPing = UniTask.ToCoroutine(async () =>
            {
                var request = new UnityWebRequest(builder.Uri)
                {
                    timeout = WebRequestTimeOutSec
                };
                try
                {
                    var result = await request.SendWebRequest();
                    var isValid = !result.IsProtocolError() && !result.IsConnectionError();
                    hasInternetConnection = isValid;
                }
                catch (Exception)
                {
                    request.Abort();
                }
            });

            yield return waitingForPing;
            if (!hasInternetConnection)
            {
                hasBeenSetup = true;
                Assert.Ignore("No internet Connection.");
            }

            var waiter = UniTask.ToCoroutine(async () =>
            {
                var result = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
                await TestContext.Out.WriteLineAsync($"Closest region for tests will be: {result.Region}");
                closestRegion = result.Region;
            });
            yield return waiter;
            hasBeenSetup = true;
            Assert.IsNotEmpty(closestRegion, "Region for tests cannot be empty.");
        }


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
                _ = await ElympicsCloudPing.ChooseClosestRegion(new[] { "non-existing-region" });
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
        });

        [UnityTest]
        public IEnumerator TestSameRegionCopiedMultipleTimes([ValueSource(typeof(ElympicsRegions), nameof(ElympicsRegions.AllAvailableRegions))] string region) => UniTask.ToCoroutine(async () =>
        {
            var result = await ElympicsCloudPing.ChooseClosestRegion(new[] { region, region });
            await TestContext.Out.WriteLineAsync($"Region: {result.Region}, latency: {result.LatencyMs} ms");
            Assert.AreEqual(region, result.Region);
        });

        [UnityTest]
        public IEnumerator TestSingleRegion([ValueSource(typeof(ElympicsRegions), nameof(ElympicsRegions.AllAvailableRegions))] string region) => UniTask.ToCoroutine(async () =>
        {
            IList<string> test = new List<string> { region };
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
            Assert.Greater(result.LatencyMs, 0.0f);
        });

        [UnityTest]
        [Ignore("Takes long to execute and yields uncertain results")]
        [Repeat(25)]
        public IEnumerator TestCorrectRegionIsFound() => UniTask.ToCoroutine(async () =>
        {
            var result = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
            await TestContext.Out.WriteLineAsync($"Region: {result.Region}, latency: {result.LatencyMs} ms");
            Assert.AreEqual(closestRegion, result.Region);
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
                _ = await ElympicsCloudPing.GetLatencyDataForRegions(new[] { "non-existing-region" });
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.AreEqual(typeof(ArgumentException), exc.GetType());
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
                Assert.Greater(latency, TimeSpan.Zero);
            }
        });

        [OneTimeTearDown]
        public void CleanUp()
        {
            hasInternetConnection = false;
            closestRegion = string.Empty;
            hasBeenSetup = false;
        }
    }
}
