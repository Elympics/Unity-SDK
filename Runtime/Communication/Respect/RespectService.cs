using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public class RespectService
    {
        private const int GetRespectForMatchRetries = 5;
        private readonly IRespectServiceWebRequest _webRequest;
        private readonly TimeSpan _minRequestInterval = TimeSpan.FromSeconds(1);
        internal static IRespectServiceWebRequest WebRequestOverride = null;

        public RespectService(IAuthManager authManager, ElympicsConfig config)
        {
            if (authManager == null)
                throw new ArgumentNullException($"{nameof(authManager)} cannot be null.");
            if (config == null)
                throw new ArgumentNullException($"{nameof(config)} cannot be null.");

            var endpoint = config.ElympicsRespectEndpoint;
            if (WebRequestOverride != null)
                _webRequest = WebRequestOverride;
            else
                _webRequest = new RespectServiceWebRequest(authManager, endpoint);
        }

        public async UniTask<GetRespectResponse> GetRespectForMatch(Guid matchId, CancellationToken ct = default)
        {
            var counter = 0;
            var currentRequestInterval = _minRequestInterval;
            while (counter < GetRespectForMatchRetries)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var result = await _webRequest.GetRespectForMatch(matchId);
                    return result;
                }
                catch (Exception)
                {
                    ct.ThrowIfCancellationRequested();
                    await UniTask.Delay(currentRequestInterval, DelayType.Realtime, PlayerLoopTiming.Update, ct);
                    ++counter;
                    var newSeconds = currentRequestInterval.TotalSeconds * 2;
                    currentRequestInterval = TimeSpan.FromSeconds(newSeconds);
                }
            }
            throw new GetRespectForMatchException(matchId);
        }
    }
}
