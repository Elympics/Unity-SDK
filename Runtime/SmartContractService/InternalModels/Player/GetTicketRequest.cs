using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal struct GetTicketRequest
    {
        public string RoomId;
        public string GameId;
        public string GameVersionId;
        public string GameData;
        public string BetAmount;

        public GetTicketRequest(string roomId, string gameId, string gameVersionId, string gameData, string betAmount)
        {
            RoomId = roomId;
            GameId = gameId;
            GameVersionId = gameVersionId;
            GameVersionId = Guid.Empty.ToString();
            GameData = gameData;
            BetAmount = betAmount;
        }
    }
}
