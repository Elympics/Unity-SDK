using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using AuthRoutes = Elympics.Models.Authentication.Routes;

#nullable enable

namespace Elympics
{
    internal class RemoteAuthClient : IAuthClient
    {
        private readonly Regex _ethAddressRegex = new("^(0x)?[0-9a-f]{40}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _ethSignatureRegex = new("^(0x)?[0-9a-f]{130}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly string _clientSecretAuthUrl;
        private readonly string _ethAddressNonceUrl;
        private readonly string _ethAddressAuthUrl;

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        public RemoteAuthClient(string authEndpoint)
        {
            var uriBuilder = new UriBuilder(authEndpoint);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.ClientSecretAuth);
            _clientSecretAuthUrl = uriBuilder.Uri.ToString();
            uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.EthAddressNonce);
            _ethAddressNonceUrl = uriBuilder.Uri.ToString();
            uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.EthAddressAuth);
            _ethAddressAuthUrl = uriBuilder.Uri.ToString();
        }

        public async UniTask<Result<AuthData, string>> AuthenticateWithClientSecret(string clientSecret, CancellationToken ct = default)
        {
            using var delayCts = new CancellationTokenSource();
            using var timer = delayCts.CancelAfterSlim(_timeout, DelayType.Realtime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, delayCts.Token);

            var putTcs = new UniTaskCompletionSource<Result<AuthenticationDataResponse, Exception>>();
            var requestModel = new ClientSecretAuthRequest { clientSecret = clientSecret };
            ElympicsWebClient.SendPutRequest<AuthenticationDataResponse>(_clientSecretAuthUrl, requestModel, null, result => putTcs.TrySetResult(result), ct: ct);
            var putTcsResult = await putTcs.Task.SuppressCancellationThrow();
            if (putTcsResult.IsCanceled is false)
                return putTcsResult.Result.IsSuccess ? Result<AuthData, string>.Success(new AuthData(putTcsResult.Result.Value, AuthType.ClientSecret)) : Result<AuthData, string>.Failure(putTcsResult.Result.Error.Message);

            return Result<AuthData, string>.Failure(ct.IsCancellationRequested ? "Request cancelled." : "Timeout.");
        }

        public async UniTask<Result<AuthData, string>> AuthenticateWithEthAddress(IEthSigner ethSigner, CancellationToken ct = default)
        {
            var ethAddress = ethSigner.Address;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (ethAddress == null)
                return Result<AuthData, string>.Failure($"Address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.Address)} cannot be null");

            var addressMatch = _ethAddressRegex.Match(ethAddress);
            if (!addressMatch.Success)
                return Result<AuthData, string>.Failure($"Invalid format for address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.Address)}: " + ethAddress);

            var addressHasPrefix = addressMatch.Groups[1].Success;
            if (!addressHasPrefix)
                ethAddress = "0x" + ethAddress;

            var nonceRequest = new EthAddressNonceRequest { address = ethAddress };

            var putTcs = new UniTaskCompletionSource<Result<EthAddressNonceResponse, Exception>>();
            ElympicsWebClient.SendPutRequest<EthAddressNonceResponse>(_ethAddressNonceUrl, nonceRequest, null, OnPutFinish, ct: ct);
            var putResult = await putTcs.Task;
            if (putResult.IsFailure)
                return Result<AuthData, string>.Failure(putResult.Error.Message);

            string? typedData;
            string? signature;
            try
            {
                typedData = ethSigner.ProvideTypedData(putResult.Value.nonce);
                signature = await ethSigner.SignAsync(typedData, ct);
            }
            catch (Exception e)
            {
                return Result<AuthData, string>.Failure(e.ToString());
            }

            if (signature == null)
                return Result<AuthData, string>.Failure($"Signature provided by {nameof(IEthSigner)}.{nameof(IEthSigner.SignAsync)} cannot be null");

            var signatureMatch = _ethSignatureRegex.Match(signature);
            if (!signatureMatch.Success)
                return Result<AuthData, string>.Failure($"Invalid format for signature provided by {nameof(IEthSigner)}.{nameof(IEthSigner.SignAsync)}: " + signature);

            var signatureHasPrefix = signatureMatch.Groups[1].Success;
            if (!signatureHasPrefix)
                signature = "0x" + signature;

            var authRequest = new EthAddressAuthRequest
            {
                typedData = typedData,
                signature = signature,
            };
            using var delayCts = new CancellationTokenSource();
            using var timer = delayCts.CancelAfterSlim(_timeout, DelayType.Realtime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, delayCts.Token);

            var postTcs = new UniTaskCompletionSource<Result<AuthenticationDataResponse, Exception>>();


            ElympicsWebClient.SendPostRequest<AuthenticationDataResponse>(_ethAddressAuthUrl, authRequest, null, OnPostFinish, ct: ct);
            var (isCanceled, results) = await postTcs.Task.SuppressCancellationThrow();
            if (isCanceled is false)
                return results.IsSuccess ? Result<AuthData, string>.Success(new AuthData(results.Value, AuthType.EthAddress)) : Result<AuthData, string>.Failure(results.Error.Message);

            return Result<AuthData, string>.Failure(ct.IsCancellationRequested ? "Request cancelled." : "Timeout.");
            void OnPostFinish(Result<AuthenticationDataResponse, Exception> result) => postTcs.TrySetResult(result);
            void OnPutFinish(Result<EthAddressNonceResponse, Exception> result) => putTcs.TrySetResult(result);
        }
        public async UniTask<Result<AuthData, string>> AuthenticateWithTelegram(ITelegramSigner telegramSigner, CancellationToken ct = default) => await UniTask.FromResult(Result<AuthData, string>.Failure($"Implicit authorization is not supported for the specified authentication type: {AuthType.Telegram}."));
    }
}
