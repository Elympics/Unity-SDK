using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    internal class PingResultResultFactoryMock : IPingResultFactory
    {
        private readonly string[] _testRegionList;
        private readonly Dictionary<string, PingResults> _mockResults = new();

        public async UniTask<PingResults> GetPingResult(string region) => await UniTask.FromResult(_mockResults[region]);

        public PingResultResultFactoryMock(string[] testRegionList)
        {
            _testRegionList = testRegionList;
            GeneratePingResults(true);
        }

        public void Reset()
        {
            _mockResults.Clear();
            GeneratePingResults(true);
        }

        public void SetInvalidRegion(string region)
        {
            _mockResults[region] = _mockResults[region] with
            {
                IsValid = false,
            };
        }

        public void SetClosestRegion(string closestRegion)
        {
            _mockResults[closestRegion] = _mockResults[closestRegion] with
            {
                LatencyMs = 1d,
            };
        }

        private void GeneratePingResults(bool valid)
        {
            var ping = 10d;
            foreach (var region in _testRegionList)
            {
                _mockResults.Add(region, new PingResults(region, ping, valid));
                ping += 10;
            }
        }
    }

}
