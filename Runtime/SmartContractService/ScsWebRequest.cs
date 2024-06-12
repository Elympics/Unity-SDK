using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using SCS.InternalModels.Player;

#nullable enable

namespace SCS
{
    internal class ScsWebRequest : IScsWebRequest
    {
        private readonly IAuthManager _authManager;
        private readonly string _chainConfigUrl;
        private readonly string _ticketUrl;
        private readonly string _getPlayerReadyUrl;
        private readonly string _getDepositUrl;
        private readonly string _postDepositAddUrl;
        private readonly string _getTransactionHistoryUrl;
        private readonly string _postWithdrawTicketUrl;

        public ScsWebRequest(IAuthManager authManager, string endPoint)
        {
            _authManager = authManager;
            var uriBuilder = new UriBuilder(endPoint);
            var initPath = uriBuilder.Path.TrimEnd('/');
            _chainConfigUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", ChainConfigRoutes.Base, ChainConfigParams.GameId));
            _ticketUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Ticket));
            _getPlayerReadyUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Ready));
            _getDepositUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Deposit));
            _postDepositAddUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.DepositAdd));
            _getTransactionHistoryUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.TransactionHistory));
            _postWithdrawTicketUrl = ElympicsWebClient.CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.DepositWithdrawTicket));
        }

        public async UniTask<GetTicketResponse> GetTicket(GetTicketRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetTicketResponse>();
            ElympicsWebClient.SendPostRequest(_ticketUrl, request, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            return await tcs.Task;
        }

        public async UniTask SendSignedTicket(SendSignedTicketRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<object>();
            ElympicsWebClient.SendPutRequest(_ticketUrl, request, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            _ = await tcs.Task;
        }

        public async UniTask<SetPlayerReadyResponse> SetPlayerReady(Guid roomId, BigInteger betAmount, Guid gameId, Guid versionId, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<SetPlayerReadyResponse>();
            var query = new Dictionary<string, string>
            {
                {
                    nameof(roomId), roomId.ToString()
                },
                {
                    nameof(betAmount), betAmount.ToString()
                },
                {
                    nameof(gameId), gameId.ToString()
                },
                {
                    nameof(versionId), versionId.ToString()
                },
            };
            ElympicsWebClient.SendGetRequest(_getPlayerReadyUrl, query, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            return await tcs.Task;
        }

        public async UniTask<IReadOnlyList<DepositState>> GetDepositStates(string gameId, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetDepositStateResponse>();
            var query = new Dictionary<string, string>
            {
                {
                    nameof(gameId), gameId
                },
            };
            ElympicsWebClient.SendGetRequest(_getDepositUrl, query, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);

            var response = await tcs.Task;
            return response.Deposits.Select(x => new DepositState(x)).ToList();
        }

        public async UniTask<IReadOnlyList<TransactionToSign>> AddDeposit(AddDepositRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<AddDepositResponse>();

            ElympicsWebClient.SendPostRequest(_postDepositAddUrl, request, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);

            var response = await tcs.Task;
            return response.TransactionsToSign.Select(x => new TransactionToSign
            {
                From = x.From,
                To = x.To,
                Data = x.Data,
            }).ToList();
        }

        public async UniTask WithdrawTicket(CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<object>();
            ElympicsWebClient.SendPostRequest(_postWithdrawTicketUrl, null, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            _ = await tcs.Task;
        }

        public async UniTask<IReadOnlyList<FinalizedTransaction>> GetUserTransactionList(string? gameId, int limit = 5, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetTransactionListResponse>();
            var query = new Dictionary<string, string>
            {
                {
                    nameof(gameId), gameId!
                },
                {
                    nameof(limit), limit.ToString()
                },
            };
            ElympicsWebClient.SendGetRequest(_getTransactionHistoryUrl, query, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            var response = await tcs.Task;

            return response.Transactions.Select(x => new FinalizedTransaction
            {
                MatchId = x.MatchId.ToString(),
                GameId = x.GameId.ToString(),
                GameName = x.GameName,
                VersionName = x.VersionName,
                Result = x.Result,
                Amount = BigInteger.Parse(x.Amount),
                State = (TransactionState)x.Status,
                ChainId = x.ChainId,
                TransactionId = x.TransactionId,
            }).ToList();
        }
        public async UniTask<GetConfigResponse> GetChainConfig(string gameId, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetConfigResponse>();
            var parameters = new Dictionary<string, string>
            {
                {
                    nameof(gameId), gameId
                },
            };
            var requestUrl = ElympicsWebClient.FillParams(_chainConfigUrl, ":", parameters);
            ElympicsWebClient.SendGetRequest(requestUrl, null, authorization, ElympicsWebClient.CreateResponseHandler(tcs), ct);
            return await tcs.Task;
        }

        private string GetAuthBearer()
        {
            var authorization = _authManager.AuthData?.BearerAuthorization ?? throw new InvalidOperationException("Authentication is required to perform the operation.");
            return authorization;
        }
    }
}
