using System;
using Cysharp.Threading.Tasks;
using NSubstitute;
using UnityEngine;

namespace Elympics
{
    public static class RespectServiceWebRequestMock
    {
        private static readonly IRespectServiceWebRequest WebRequest;
        private const string Response = "{\"MatchId\":\"6145a049-e4e3-4ca0-85db-ae7e5a57739f\",\"Respect\":19}";
        internal static IRespectServiceWebRequest MockRespectServiceWebRequest() => WebRequest;

        static RespectServiceWebRequestMock() => WebRequest = CreateMockForWebRequest();
        private static IRespectServiceWebRequest CreateMockForWebRequest()
        {
            var wr = Substitute.For<IRespectServiceWebRequest>();

            _ = wr.GetRespectForMatch(Arg.Any<Guid>()).Returns(_ =>
            {
                var result = JsonUtility.FromJson<GetRespectResponse>(Response);
                return UniTask.FromResult(result);
            });

            return wr;
        }
    }
}
