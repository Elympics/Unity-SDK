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
            _chainConfigUrl = CombinePath(uriBuilder, initPath, string.Join("/", ChainConfigRoutes.Base, ChainConfigParams.GameId));
            _ticketUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Ticket));
            _getPlayerReadyUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Ready));
            _getDepositUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.Deposit));
            _postDepositAddUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.DepositAdd));
            _getTransactionHistoryUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.TransactionHistory));
            _postWithdrawTicketUrl = CombinePath(uriBuilder, initPath, string.Join("/", PlayerRoutes.Base, PlayerRoutes.DepositWithdrawTicket));
        }

        private static string CombinePath(UriBuilder uriBuilder, string initPath, string path)
        {
            uriBuilder.Path = string.Join("/", initPath, path);
            return uriBuilder.Uri.ToString();
        }

        private static string FillParams(string url, Dictionary<string, string> parameters)
        {
            var newUrl = url;
            foreach ((var key, var value) in parameters)
            {
                var replacedString = $"/:{key}";
                if (!newUrl.Contains(replacedString))
                    throw new InvalidOperationException($"Invalid parameters in request: {url} | {key}");
                newUrl = newUrl.Replace(replacedString, $"/{value}");
            }
            return newUrl;
        }

        public async UniTask<GetTicketResponse> GetTicket(GetTicketRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<GetTicketResponse>();
            ElympicsWebClient.SendPostRequest(_ticketUrl, request, authorization, CreateResponseHandler(tcs), ct);
            return await tcs.Task;
        }

        public async UniTask SendSignedTicket(SendSignedTicketRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<object>();
            ElympicsWebClient.SendPutRequest(_ticketUrl, request, authorization, CreateResponseHandler(tcs), ct);
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
            ElympicsWebClient.SendGetRequest(_getPlayerReadyUrl, query, authorization, CreateResponseHandler(tcs), ct);
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
            ElympicsWebClient.SendGetRequest(_getDepositUrl, query, authorization, CreateResponseHandler(tcs), ct);

            var response = await tcs.Task;
            return response.Deposits.Select(x => new DepositState(x)).ToList();
        }

        public async UniTask<IReadOnlyList<TransactionToSign>> AddDeposit(AddDepositRequest request, CancellationToken ct = default)
        {
            var authorization = GetAuthBearer();
            var tcs = new UniTaskCompletionSource<AddDepositResponse>();

            ElympicsWebClient.SendPostRequest(_postDepositAddUrl, request, authorization, CreateResponseHandler(tcs), ct);

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
            ElympicsWebClient.SendPostRequest(_postWithdrawTicketUrl, null, authorization, CreateResponseHandler(tcs), ct);
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
            ElympicsWebClient.SendGetRequest(_getTransactionHistoryUrl, query, authorization, CreateResponseHandler(tcs), ct);
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
            var requestUrl = FillParams(_chainConfigUrl, parameters);
            ElympicsWebClient.SendGetRequest(requestUrl, null, authorization, CreateResponseHandler(tcs), ct);
            return await tcs.Task;
        }

        private string GetAuthBearer()
        {
            var authorization = _authManager.AuthData?.BearerAuthorization ?? throw new InvalidOperationException("Authentication is required to perform the operation.");
            return authorization;
        }

        private Action<Result<TResult, Exception>> CreateResponseHandler<TResult>(UniTaskCompletionSource<TResult> tcs) =>
            result => _ = result.IsSuccess ? tcs.TrySetResult(result.Value) : tcs.TrySetException(result.Error);
    }
}
