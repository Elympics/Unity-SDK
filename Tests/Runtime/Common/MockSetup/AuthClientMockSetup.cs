using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using NSubstitute;

namespace Elympics
{
    internal static class AuthClientMockSetup
    {
        public static IAuthClient CreateSuccessIAuthClient(this IAuthClient mock, Guid userId, string nickname)
        {
            const string token = "eyJhbGciOiJub25lIn0.eyJleHAiOjEwMDM5OTk5OTk5fQ."; // none-alg (but valid) token with exp set to year 2288
            var clientSecretTaskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(userId, token, nickname, AuthType.ClientSecret)));
            var ethAddressTaskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(userId, token, nickname, AuthType.EthAddress)));
            _ = mock.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(clientSecretTaskResult);
            _ = mock.AuthenticateWithEthAddress(Arg.Any<IEthSigner>(), Arg.Any<CancellationToken>()).Returns(ethAddressTaskResult);
            return mock;
        }

        public static IAuthClient CreateFailureIAuthClient(this IAuthClient mock)
        {
            var clientSecretTaskResult = UniTask.FromResult(Result<AuthData, string>.Failure("Failed to authenticate with clientSecret"));
            var ethAdressTaskResult = UniTask.FromResult(Result<AuthData, string>.Failure("Failed to authenticate with ethAdress"));
            _ = mock.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(clientSecretTaskResult);
            _ = mock.AuthenticateWithEthAddress(Arg.Any<IEthSigner>(), Arg.Any<CancellationToken>()).Returns(ethAdressTaskResult);
            return mock;
        }
    }
}
