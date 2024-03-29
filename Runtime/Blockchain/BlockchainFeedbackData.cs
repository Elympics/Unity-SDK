namespace Elympics
{
    public struct BlockchainFeedbackData
    {
        public string TransactionHash { get; }
        public string ErrorCode { get; }

        public BlockchainFeedbackData(string transactionHash, string errorCode)
        {
            TransactionHash = transactionHash;
            ErrorCode = errorCode;
        }
    }
}
