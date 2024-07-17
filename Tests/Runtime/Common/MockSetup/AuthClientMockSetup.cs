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
        public static IAuthClient CreateDefaultIAuthClient(string jwt, Guid userId, string nickname, AuthType authType)
        {
            var token = EncodeJwtFromJson(jwt);
            var taskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(userId, token, nickname, authType)));
            var ac = Substitute.For<IAuthClient>();
            _ = ac.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(taskResult);
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

