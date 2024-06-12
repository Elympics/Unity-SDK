using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Elympics.InternalModels.Respect.Player;

namespace Elympics
{
    public class RespectServiceWebRequest : IRespectServiceWebRequest
    {
        private readonly IAuthManager _authManager;
        private readonly string _getMatchRespectUrl;
        public RespectServiceWebRequest(IAuthManager authManager, string endPoint)
        {
            _authManager = authManager;
            var uriBuilder = new UriBuilder(endPoint);
            var initPath = uriBuilder.Path.TrimEnd('/');
            _getMatchRespectUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", RespectRoutes.Base, RespectRoutes.MatchRespect, RespectParams.MatchId));
        }
        public async UniTask<GetRespectResponse> GetRespectForMatch(Guid matchId)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetRespectResponse>();
            var query = new Dictionary<string, string>
            {
                {
                    nameof(matchId), matchId.ToString()
                },
            };
            var requestUrl = ElympicsWebClient.FillParams(_getMatchRespectUrl, ":", query);
            ElympicsWebClient.SendGetRequest(requestUrl, null, authorization, ElympicsWebClient.CreateResponseHandler(tcs));
            return await tcs.Task;
        }

        private string GetAuthBearer()
        {
            var authorization = _authManager.AuthData?.BearerAuthorization ?? throw new InvalidOperationException("Authentication is required to perform the operation.");
            return authorization;
        }
    }
}
