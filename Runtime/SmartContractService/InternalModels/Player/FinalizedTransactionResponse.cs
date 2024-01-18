using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class FinalizedTransactionResponse
    {
        public Guid MatchId;
        public Guid GameId;
        public string GameName;
        public string VersionName;
        public int Result;
        public string Amount;
        public int Status;
        public int ChainId;
        public string TransactionId;
    }
}
