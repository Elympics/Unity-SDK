using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsCloudPing
	{
		private const           int                             IterationNumber   = 3;
		private const           int                             TimeOut           = 2;
		private const           string                          PingRoute         = "api/ping";
		private static readonly ISet<string>                    DistinctRegions   = new HashSet<string>();
		private static readonly Dictionary<string, LatencyData> RegionLatencyData = new Dictionary<string, LatencyData>();

		public static async UniTask<(string Region, float LatencyMs)> ChooseClosestRegion(IList<string> regions)
		{
			if (regions == null)
				throw new ArgumentNullException(nameof(regions));

			if (regions.Count == 0)
				throw new ArgumentException("Regions list cannot be empty.", nameof(regions));

			DistinctRegions.Clear();

			foreach (var region in regions)
			{
				if (!ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl.ContainsKey(region))
					throw new ArgumentException($"Could not find Google Cloud url for region {region}", nameof(regions));
				DistinctRegions.Add(region);
			}

			var pingsTasks = new List<UniTask<PingResults>>();

			foreach (var region in DistinctRegions)
				for (var i = 0; i < IterationNumber; i++)
					pingsTasks.Add(GetPingResult(region));

			var results = await UniTask.WhenAll(pingsTasks);
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
				throw new ElympicsException("Network error");

			var closestRegion = string.Empty;
			var minLatencyMs = double.MaxValue;
			foreach (var latencyData in RegionLatencyData)
				if (latencyData.Value.LatencyMedian < minLatencyMs)
				{
					minLatencyMs = latencyData.Value.LatencyMedian;
					closestRegion = latencyData.Key;
				}

			return (Region: closestRegion, LatencyMs: (float)minLatencyMs);
		}

		private struct PingResults
		{
			public readonly string RegionName;
			public readonly double LatencyMs;
			public readonly bool   IsValid;

			public PingResults(string regionName, double latencyMs, bool isValid)
			{
				RegionName = regionName;
				LatencyMs = latencyMs;
				IsValid = isValid;
			}
		}

		private static async UniTask<PingResults> GetPingResult(string region)
		{
			var url = ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl[region];
			var uriBuilder = new UriBuilder(url)
			{
				Path = PingRoute
			};
			var stopwatch = new Stopwatch();
			var webRequest = UnityWebRequest.Get(uriBuilder.Uri);
			webRequest.timeout = TimeOut;
			stopwatch.Start();
			var results = await webRequest.SendWebRequest();
			stopwatch.Stop();
			var isValid = !results.IsProtocolError() && !results.IsConnectionError();
			return new PingResults(region, stopwatch.Elapsed.TotalMilliseconds, isValid);
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
