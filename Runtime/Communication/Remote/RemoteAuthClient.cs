using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Elympics.Models.Authentication;
using AuthRoutes = Elympics.Models.Authentication.Routes;

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

        public async void AuthenticateWithEthAddress(IEthSigner ethSigner, Action<Result<AuthData, string>> onResult, CancellationToken ct = default)
        {
            var ethAddress = await ethSigner.ProvideAddressAsync(ct: ct);
            if (ethAddress == null)
            {
                onResult(Result<AuthData, string>.Failure(
                    $"Address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.ProvideAddressAsync)} cannot be null"));
                return;
            }

            var addressMatch = _ethAddressRegex.Match(ethAddress);
            if (!addressMatch.Success)
            {
                onResult(Result<AuthData, string>.Failure(
                    $"Invalid format for address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.ProvideAddressAsync)}: "
                    + ethAddress));
                return;
            }

            var addressHasPrefix = addressMatch.Groups[1].Success;
            if (!addressHasPrefix)
                ethAddress = "0x" + ethAddress;

            var nonceRequest = new EthAddressNonceRequest { address = ethAddress };
            ElympicsWebClient.SendPutRequest<string>(_ethAddressNonceUrl, nonceRequest, callback: OnNonceResponse, ct: ct);

            async void OnNonceResponse(Result<string, Exception> result)
            {
                if (result.IsFailure)
                {
                    onResult(Result<AuthData, string>.Failure(result.Error.Message));
                    return;
                }

                string hexEncodedMessage;
                string signature;
                try
                {
                    hexEncodedMessage = HexEncodeUtf8String(ethSigner.ProvideMessage(result.Value));
                    signature = await ethSigner.SignAsync(hexEncodedMessage, ct);
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
                    address = ethAddress,
                    msg = hexEncodedMessage,
                    sig = signature,
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

        private static string HexEncodeUtf8String(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            return string.Concat(bytes.Select(x => x.ToString("X2")));
        }
    }
}
