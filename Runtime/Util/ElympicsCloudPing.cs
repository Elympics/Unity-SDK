using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    public static class ElympicsCloudPing
    {
        private const int IterationNumber = 3;
        private static readonly Dictionary<string, LatencyData> RegionLatencyData = new();
        internal static IPingResultFactory PingResultFactory = new GoogleCloudPingResultFactory();

        public static async UniTask<(string Region, float LatencyMs)> ChooseClosestRegion(IList<string> regions)
        {
            var closestRegion = string.Empty;
            var minLatencyMs = double.MaxValue;
            foreach (var (region, latencyMs) in await GetLatencyDataForRegions(regions))
                if (latencyMs.TotalMilliseconds < minLatencyMs)
                {
                    minLatencyMs = latencyMs.TotalMilliseconds;
                    closestRegion = region;
                }

            return (Region: closestRegion, LatencyMs: (float)minLatencyMs);
        }

        public static async UniTask<Dictionary<string, TimeSpan>> GetLatencyDataForRegions(IList<string> regions)
        {
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));

            if (regions.Count == 0)
                throw new ArgumentException("Regions list cannot be empty.", nameof(regions));

            var distinctRegions = new HashSet<string>();

            foreach (var region in regions)
            {
                if (!ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl.ContainsKey(region))
                    throw new ArgumentException($"Could not find Google Cloud URL for region {region}", nameof(regions));
                _ = distinctRegions.Add(region);
            }

            var results = new List<PingResults>();
            foreach (var region in distinctRegions)
                for (var i = 0; i < IterationNumber; i++)
                {
                    var result = await PingResultFactory.GetPingResult(region);
                    results.Add(result);
                }

            RegionLatencyData.Clear();
            foreach (var pingResult in results)
            {
                if (!pingResult.IsValid)
                    continue;

                if (!RegionLatencyData.ContainsKey(pingResult.RegionName))
                    RegionLatencyData.Add(pingResult.RegionName, new LatencyData(pingResult.LatencyMs));
                else
                    RegionLatencyData[pingResult.RegionName].AddLatency(pingResult.LatencyMs);
            }

            if (RegionLatencyData.Count == 0)
                throw new ElympicsException("No valid results received. Possible network error.");

            return RegionLatencyData.ToDictionary(entry => entry.Key, entry => TimeSpan.FromMilliseconds(entry.Value.LatencyMedian));
        }

        private class LatencyData
        {
            public double LatencyMedian { get; private set; }

            private readonly List<double> _latencies;

            public LatencyData(double latency)
            {
                _latencies = new List<double>();
                AddLatency(latency);
            }

            public void AddLatency(double latency)
            {
                _latencies.Add(latency);
                _latencies.Sort();
                if (_latencies.Count % 2 != 0)
                {
                    LatencyMedian = _latencies[_latencies.Count / 2];
                }
                else
                {
                    var value1 = _latencies[_latencies.Count / 2];
                    var value2 = _latencies[_latencies.Count / 2 - 1];
                    LatencyMedian = (value1 + value2) / 2;
                }
            }
        }
    }
}
