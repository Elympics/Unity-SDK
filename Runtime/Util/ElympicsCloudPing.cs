﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsCloudPing
	{
		private const           int                             IterationNumber    = 3;
		private const           int                             TimeOut            = 2;
		private const           string                          _pingRoute         = "api/ping";
		private static readonly ISet<string>                    _distinctRegions   = new HashSet<string>();
		private static readonly Dictionary<string, LatencyData> _regionLatencyData = new Dictionary<string, LatencyData>();

		public static async UniTask<string> ChooseClosestRegion(IList<string> regions)
		{
			if (regions == null)
				throw new ArgumentNullException(nameof(regions));

			if (regions.Count == 0)
				throw new ArgumentException("Regions list cannot be empty.");

			_distinctRegions.Clear();

			foreach (var region in regions)
			{
				if (!ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl.ContainsKey(region))
					throw new ArgumentException($"Could not find Google Cloud url for region {region}", nameof(regions));
				_distinctRegions.Add(region);
			}

			if (_distinctRegions.Count == 1)
				return regions[0];

			List<UniTask<PingResults>> pingsTasks = new List<UniTask<PingResults>>();

			foreach (var region in _distinctRegions)
				for (var i = 0; i < IterationNumber; i++)
					pingsTasks.Add(GetPingResult(region));

			var results = await UniTask.WhenAll(pingsTasks);
			_regionLatencyData.Clear();
			foreach (var pingResult in results)
			{
				if (!pingResult.IsValid)
					continue;

				if (!_regionLatencyData.ContainsKey(pingResult.RegionName))
				{
					_regionLatencyData.Add(pingResult.RegionName, new LatencyData(pingResult.Latency));
				}
				else
				{
					_regionLatencyData[pingResult.RegionName].AddLatency(pingResult.Latency);
				}
			}

			string closestRegion = string.Empty;
			double minLatency = double.MaxValue;
			foreach (var latencyData in _regionLatencyData)
			{
				if (latencyData.Value.LatencyMedian < minLatency)
				{
					minLatency = latencyData.Value.LatencyMedian;
					closestRegion = latencyData.Key;
				}
			}

			return closestRegion;
		}

		public struct PingResults
		{
			public readonly string RegionName;
			public readonly double Latency;
			public readonly bool   IsValid;

			public PingResults(string regionName, double latency, bool isValid)
			{
				RegionName = regionName;
				Latency = latency;
				IsValid = isValid;
			}
		}

		private static async UniTask<PingResults> GetPingResult(string region)
		{
			var url = ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl[region];
			var uriBuilder = new UriBuilder(url)
			{
				Path = _pingRoute
			};
			var stopwatch = new Stopwatch();
			var webRequest = UnityWebRequest.Get(uriBuilder.Uri);
			webRequest.timeout = TimeOut;
			stopwatch.Start();
			var resutls = await webRequest.SendWebRequest();
			stopwatch.Stop();
			bool isValid = !resutls.IsProtocolError() && !resutls.IsConnectionError();
			return new PingResults(region, stopwatch.Elapsed.TotalMilliseconds, isValid);
		}

		private class LatencyData
		{
			private readonly List<double> _latencies;

			public LatencyData(double latency)
			{
				_latencies = new List<double>();
				AddLatency(latency);
			}

			public double LatencyMedian { get; private set; }


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