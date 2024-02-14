using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal struct GetTicketRequest
    {
        public string RoomId;
        public string GameId;
        public string GameName;
        public string GameVersion;
        public string GameVersionId;
        public string GameData;
        public string BetAmount;

        public GetTicketRequest(
            string roomId,
            string gameId,
            string gameName,
            string gameVersion,
            string gameVersionId,
            string gameData,
            string betAmount)
        {
            RoomId = roomId;
            GameId = gameId;
            GameName = gameName;
            GameVersion = gameVersion;
            GameVersionId = gameVersionId;
            GameData = gameData;
            BetAmount = betAmount;
        }
    }
}
