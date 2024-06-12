using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using SCS.InternalModels.Player;

#nullable enable

namespace SCS
{
    internal interface IScsWebRequest
    {
        UniTask<GetTicketResponse> GetTicket(GetTicketRequest request, CancellationToken ct = default);
        UniTask SendSignedTicket(SendSignedTicketRequest request, CancellationToken ct = default);
        UniTask<SetPlayerReadyResponse> SetPlayerReady(Guid roomId, BigInteger betAmount, Guid gameId, Guid versionId, CancellationToken ct = default);
        UniTask<IReadOnlyList<DepositState>> GetDepositStates(string gameId, CancellationToken ct = default);

        UniTask<IReadOnlyList<TransactionToSign>> AddDeposit(AddDepositRequest request, CancellationToken ct = default);
        UniTask WithdrawTicket(CancellationToken ct = default);
        UniTask<IReadOnlyList<FinalizedTransaction>> GetUserTransactionList(string? gameId, int limit = 5, CancellationToken ct = default);
        UniTask<GetConfigResponse> GetChainConfig(string gameId, CancellationToken ct = default);
    }
}
