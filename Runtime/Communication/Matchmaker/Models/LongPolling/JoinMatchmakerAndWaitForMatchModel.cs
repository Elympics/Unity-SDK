using System;

namespace Elympics.Models.Matchmaking.LongPolling
{
    public static class JoinMatchmakerAndWaitForMatchModel
    {
        public static class ErrorCodes
        {
            public const string OpponentNotFound = "Opponent not found";
        }

        [Serializable]
        public class Request : ApiRequest
        {
            public string GameId;
            public string GameVersion;
            public byte[] GameEngineData;
            public float[] MatchmakerData;
            public string QueueName;
            public string RegionName;

            public Request()
            { }

            public Request(JoinMatchmakerData joinMatchmakerData)
            {
                GameId = joinMatchmakerData.GameId.ToString();
                GameVersion = joinMatchmakerData.GameVersion;
                MatchmakerData = joinMatchmakerData.MatchmakerData;
                GameEngineData = joinMatchmakerData.GameEngineData;
                QueueName = joinMatchmakerData.QueueName;
                RegionName = joinMatchmakerData.RegionName;
            }
        }

        [Serializable]
        public class Response : ApiResponse
        {
            public string MatchId;
        }
    }
}
