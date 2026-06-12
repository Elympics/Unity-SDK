#nullable enable

using System;

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class GetTransactionListResponse
    {
        public FinalizedTransactionResponse[] Transactions;
    }
}
