using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
namespace Elympics
{
    internal static class AuthClientMockSetup
    {
        public static IAuthClient CreateSuccessIAuthClient(string jwt, Guid userId, string nickname)
        {
            var token = EncodeJwtFromJson(jwt);
            var clientSecretTaskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(userId, token, nickname, AuthType.ClientSecret)));
            var ethAdressTaskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(userId, token, nickname, AuthType.EthAddress)));
            var ac = Substitute.For<IAuthClient>();
            _ = ac.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(clientSecretTaskResult);
            _ = ac.AuthenticateWithEthAddress(Arg.Any<IEthSigner>(), Arg.Any<CancellationToken>()).Returns(ethAdressTaskResult);

            return ac;
        }
        public static IAuthClient CreateFailureIAuthClient()
        {
            var clientSecretTaskResult = UniTask.FromResult(Result<AuthData, string>.Failure("Failed to authenticate with clientSecret"));
            var ethAdressTaskResult = UniTask.FromResult(Result<AuthData, string>.Failure("Failed to authenticate with ethAdress"));
            var ac = Substitute.For<IAuthClient>();
            _ = ac.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(clientSecretTaskResult);
            _ = ac.AuthenticateWithEthAddress(Arg.Any<IEthSigner>(), Arg.Any<CancellationToken>()).Returns(ethAdressTaskResult);
            return ac;
        }
        private static string EncodeJwtFromJson(string json)
        {
            var jwtObject = JObject.Parse(json);

            var expireTime = DateTime.UtcNow.AddHours(1);
            var epochTimeSpan = expireTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var epochTime = (long)epochTimeSpan.TotalSeconds;
            var header = jwtObject["header"]!.ToString(Formatting.None);
            jwtObject!["payload"]!["exp"] = epochTime;
            var payload = jwtObject["payload"]!.ToString(Formatting.None);
            var signature = jwtObject["signature"]!.ToString();

            var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
            var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var encodedSignature = Base64UrlEncode(Encoding.UTF8.GetBytes(signature));

            return $"{encodedHeader}.{encodedPayload}.{encodedSignature}";
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Replace('+', '-'); // Replace '+' with '-'
            output = output.Replace('/', '_'); // Replace '/' with '_'
            output = output.TrimEnd('='); // Remove any trailing '='
            return output;
        }
    }
}
