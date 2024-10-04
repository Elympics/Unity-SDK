using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SCS.InternalModels.Player;

#nullable enable

namespace SCS
{
    public interface ISmartContractService
    {
        [PublicAPI] ChainConfig? CurrentChain { get; }
        [PublicAPI] UniTask<GetConfigResponse> GetChainConfigForGame(string gameId);

        [PublicAPI] void RegisterWallet(IWallet wallet);
        [PublicAPI] UniTask<IReadOnlyList<DepositState>> GetDepositState(string gameId);
        [PublicAPI] UniTask AddDeposit(string gameId, BigInteger amount);
        [PublicAPI] UniTask<IReadOnlyList<FinalizedTransaction>> GetUserTransactionsList(string? gameId, int limit = 5);
        [PublicAPI] public UniTask Initialize();
        internal UniTask<string> SignTypedDataMessage(string message);
        internal UniTask<string> SetAllowance(string from, string to, string data);
        internal UniTask<SetPlayerReadyResponse> SetPlayerReady(Guid roomId, BigInteger betAmount);
        internal UniTask<GetTicketResponse> GetTicket(Guid roomId, BigInteger betAmount, string gameData);
        internal UniTask SendSignedTicket(string nonce, string signedMessage);
    }
}
