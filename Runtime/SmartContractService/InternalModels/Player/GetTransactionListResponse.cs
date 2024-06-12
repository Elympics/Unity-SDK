using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class GetTransactionListResponse
    {
        public FinalizedTransactionResponse[] Transactions;
    }
}
