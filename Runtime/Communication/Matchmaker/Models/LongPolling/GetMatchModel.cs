using System;
using System.Collections.Generic;

namespace Elympics.Models.Matchmaking.LongPolling
{
    public static class GetMatchModel
    {
        public static class ErrorCodes
        {
            public const string NotInDesiredState = "Not in desired state";
        }

        [Serializable]
        public class Request : ApiRequest
        {
            public string MatchId;
            public GetMatchDesiredState DesiredState;
        }

        [Serializable]
        public class Response : ApiResponse
        {
            public string MatchId;
            public string ServerAddress;
            public string TcpUdpServerAddress;
            public string WebServerAddress;
            public string UserSecret;
            public List<string> MatchedPlayersId;
            public UserOrBotData UserData;
        }
    }
}
