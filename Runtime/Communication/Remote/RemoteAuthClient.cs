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

        public void AuthenticateWithClientSecret(string clientSecret, Action<Result<AuthData, string>> onResult, CancellationToken ct = default)
        {
            void OnResponse(Result<AuthenticationDataResponse, Exception> result)
            {
                onResult(result.IsSuccess
                    ? Result<AuthData, string>.Success(new AuthData(result.Value, AuthType.ClientSecret))
                    : Result<AuthData, string>.Failure(result.Error.Message));
            }

            var requestModel = new ClientSecretAuthRequest { clientSecret = clientSecret };
            ElympicsWebClient.SendPutRequest<AuthenticationDataResponse>(_clientSecretAuthUrl, requestModel, callback: OnResponse, ct: ct);
        }

        public void AuthenticateWithEthAddress(IEthSigner ethSigner, Action<Result<AuthData, string>> onResult, CancellationToken ct = default)
        {
            var ethAddress = ethSigner.Address;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (ethAddress == null)
            {
                onResult(Result<AuthData, string>.Failure(
                    $"Address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.Address)} cannot be null"));
                return;
            }

            var addressMatch = _ethAddressRegex.Match(ethAddress);
            if (!addressMatch.Success)
            {
                onResult(Result<AuthData, string>.Failure(
                    $"Invalid format for address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.Address)}: "
                    + ethAddress));
                return;
            }

            var addressHasPrefix = addressMatch.Groups[1].Success;
            if (!addressHasPrefix)
                ethAddress = "0x" + ethAddress;

            var nonceRequest = new EthAddressNonceRequest { address = ethAddress };
            ElympicsWebClient.SendPutRequest<EthAddressNonceResponse>(_ethAddressNonceUrl, nonceRequest, callback: r => OnNonceResponse(r).Forget(), ct: ct);

            async UniTaskVoid OnNonceResponse(Result<EthAddressNonceResponse, Exception> result)
            {
                if (result.IsFailure)
                {
                    onResult(Result<AuthData, string>.Failure(result.Error.Message));
                    return;
                }

                string? typedData;
                string? signature;
                try
                {
                    typedData = ethSigner.ProvideTypedData(result.Value.nonce);
                    signature = await ethSigner.SignAsync(typedData, ct);
                }
                catch (Exception e)
                {
                    onResult(Result<AuthData, string>.Failure(e.ToString()));
                    return;
                }

                if (signature == null)
                {
                    onResult(Result<AuthData, string>.Failure(
                        $"Signature provided by {nameof(IEthSigner)}.{nameof(IEthSigner.SignAsync)} cannot be null"));
                    return;
                }

                var signatureMatch = _ethSignatureRegex.Match(signature);
                if (!signatureMatch.Success)
                {
                    onResult(Result<AuthData, string>.Failure(
                        $"Invalid format for signature provided by {nameof(IEthSigner)}.{nameof(IEthSigner.SignAsync)}: "
                        + signature));
                    return;
                }

                var signatureHasPrefix = signatureMatch.Groups[1].Success;
                if (!signatureHasPrefix)
                    signature = "0x" + signature;

                var authRequest = new EthAddressAuthRequest
                {
                    typedData = typedData,
                    signature = signature,
                };
                ElympicsWebClient.SendPostRequest<AuthenticationDataResponse>(_ethAddressAuthUrl, authRequest, callback: OnAuthResponse, ct: ct);
            }

            void OnAuthResponse(Result<AuthenticationDataResponse, Exception> result)
            {
                onResult(result.IsSuccess
                    ? Result<AuthData, string>.Success(new AuthData(result.Value, AuthType.EthAddress))
                    : Result<AuthData, string>.Failure(result.Error.Message));
            }
        }
    }
}
