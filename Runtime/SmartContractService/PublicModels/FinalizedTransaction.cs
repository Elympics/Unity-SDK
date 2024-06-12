using System.Numerics;

namespace SCS
{
    public enum TransactionState
    {
        Pending = 0,
        Finished = 1,
        Failed = 2,
    }

    public class FinalizedTransaction
    {
        public string MatchId;
        public string GameId;
        public string GameName;
        public string VersionName;
        public int Result;
        public BigInteger Amount;
        public TransactionState State;
        public int ChainId;
        public string TransactionId;
    }
}
