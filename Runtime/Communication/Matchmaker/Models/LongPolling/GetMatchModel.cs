using System;
using System.Collections.Generic;

namespace Elympics.Models.Matchmaking.LongPolling
{
    public static class GetMatchModel
    {
        [Serializable]
        public class Response : ApiResponse
        {
            public string MatchId;
            public string ServerAddress;
            public string QueueName;
            public string RegionName;
            public string TcpUdpServerAddress;
            public string WebServerAddress;
            public string UserSecret;
            public List<string> MatchedPlayersId;
            public UserOrBotData UserData;
        }
    }
}
