using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Elympics
{
    internal class GoogleCloudPingResultFactory : IPingResultFactory
    {
        private const string PingRoute = "api/ping";
        private const int TimeOut = 2;

        public async UniTask<PingResults> GetPingResult(string region)
        {
            var url = ElympicsRegionToGCRegionMapper.ElympicsRegionToGCRegionPingUrl[region];
            var uriBuilder = new UriBuilder(url)
            {
                Path = PingRoute
            };
            var stopwatch = new Stopwatch();
            var webRequest = UnityWebRequest.Get(uriBuilder.Uri);
            webRequest.timeout = TimeOut;
            var isValid = true;
            stopwatch.Start();
            try
            {
                _ = await webRequest.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                isValid = false;
            }
            finally
            {
                stopwatch.Stop();
            }
            return new PingResults(region, stopwatch.Elapsed.TotalMilliseconds, isValid);
        }
    }

}
