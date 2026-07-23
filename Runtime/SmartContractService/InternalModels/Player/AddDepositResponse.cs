#nullable enable

using System;

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class AddDepositResponse
    {
        public TransactionToSignResult[]? TransactionsToSign;

        public AddDepositResponse(TransactionToSignResult[]? transactionsToSign) => TransactionsToSign = transactionsToSign;
    }
}
