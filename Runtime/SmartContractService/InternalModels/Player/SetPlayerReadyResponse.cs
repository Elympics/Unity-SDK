using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class SetPlayerReadyResponse
    {
        public bool Allow;
        public string? RejectReason;
        public TransactionToSignResult[]? TransactionsToSign;

        public SetPlayerReadyResponse(bool allow, string? rejectReason, TransactionToSignResult[]? transactionsToSign)
        {
            Allow = allow;
            RejectReason = rejectReason;
            TransactionsToSign = transactionsToSign;
        }
    }
}
