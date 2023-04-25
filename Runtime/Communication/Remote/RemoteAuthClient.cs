using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Elympics.Models.Authentication;
using AuthRoutes = Elympics.Models.Authentication.Routes;

namespace Elympics
{
	internal class RemoteAuthClient : IAuthClient
	{
		// TODO: softcode this address ~dsygocki 2023-04-19
		private const string AuthBaseUrl = "https://api.elympics.cc/v2/auth";

		private readonly string _clientSecretAuthUrl;
		private readonly string _ethAddressNonceUrl;
		private readonly string _ethAddressAuthUrl;

		internal RemoteAuthClient()
		{
			var uriBuilder = new UriBuilder(AuthBaseUrl);
			var oldPath = uriBuilder.Path.TrimEnd('/');
			uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.ClientSecretAuth);
			_clientSecretAuthUrl = uriBuilder.Uri.ToString();
			uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.EthAddressNonce);
			_ethAddressNonceUrl = uriBuilder.Uri.ToString();
			uriBuilder.Path = string.Join("/", oldPath, AuthRoutes.Base, AuthRoutes.EthAddressAuth);
			_ethAddressAuthUrl = uriBuilder.Uri.ToString();
		}

		public void AuthenticateWithClientSecret(string clientSecret, Action<Result<AuthenticationData, string>> onResult, CancellationToken ct = default)
		{
			void OnResponse(Result<AuthenticationDataResponse, Exception> result)
			{
				onResult(result.IsSuccess
					? Result<AuthenticationData, string>.Success(new AuthenticationData(result.Value))
					: Result<AuthenticationData, string>.Failure(result.Error.Message));
			}

			var requestModel = new ClientSecretAuthRequest { clientSecret = clientSecret };
			ElympicsWebClient.SendJsonPutRequest<AuthenticationDataResponse>(_clientSecretAuthUrl, requestModel, callback: OnResponse, ct: ct);
		}

		public async void AuthenticateWithEthAddress(IEthSigner ethSigner, Action<Result<AuthenticationData, string>> onResult, CancellationToken ct = default)
		{
			var ethAddress = await ethSigner.ProvideAddressAsync(ct: ct);
			if (ethAddress == null)
			{
				onResult(Result<AuthenticationData, string>.Failure(
					$"Address provided by {nameof(IEthSigner)}.{nameof(IEthSigner.ProvideAddressAsync)} cannot be null"));
				return;
			}

			ethAddress = ethAddress.StartsWith("0x", true, CultureInfo.InvariantCulture)
				? ethAddress.Substring(2)
				: ethAddress;

			var nonceRequest = new EthAddressNonceRequest { address = ethAddress };
			ElympicsWebClient.SendJsonPutRequest<string>(_ethAddressNonceUrl, nonceRequest, callback: OnNonceResponse, ct: ct);

			async void OnNonceResponse(Result<string, Exception> result)
			{
				if (result.IsFailure)
				{
					onResult(Result<AuthenticationData, string>.Failure(result.Error.Message));
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
					onResult(Result<AuthenticationData, string>.Failure(e.ToString()));
					return;
				}

				if (signature == null)
				{
					onResult(Result<AuthenticationData, string>.Failure(
						$"Signature provided by {nameof(IEthSigner)}.{nameof(IEthSigner.SignAsync)} cannot be null"));
					return;
				}

				if (signature.StartsWith("0x", true, CultureInfo.InvariantCulture))
					signature = signature.Substring(2);

				var authRequest = new EthAddressAuthRequest
				{
					address = ethAddress,
					msg = hexEncodedMessage,
					sig = signature
				};
				ElympicsWebClient.SendJsonPostRequest<AuthenticationDataResponse>(_ethAddressAuthUrl, authRequest, callback: OnAuthResponse, ct: ct);
			}

			void OnAuthResponse(Result<AuthenticationDataResponse, Exception> result)
			{
				onResult(result.IsSuccess
					? Result<AuthenticationData, string>.Success(new AuthenticationData(result.Value))
					: Result<AuthenticationData, string>.Failure(result.Error.Message));
			}
		}

		private static string HexEncodeUtf8String(string str)
		{
			var bytes = Encoding.UTF8.GetBytes(str);
			return string.Concat(bytes.Select(x => x.ToString("X2")));
		}
	}
}
