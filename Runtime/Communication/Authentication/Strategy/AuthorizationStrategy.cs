using System;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
namespace Elympics
{
    internal abstract class AuthorizationStrategy
    {
        protected readonly IAuthClient AuthClient;
        protected string ClientSecret;
        protected ElympicsEthSigner EthSigner;
        protected ITelegramSigner TelegramSigner;
        internal AuthorizationStrategy(IAuthClient authClient, string clientSecret, ElympicsEthSigner ethSigner, ITelegramSigner telegramSigner)
        {
            AuthClient = authClient;
            ClientSecret = clientSecret;
            EthSigner = ethSigner;
            TelegramSigner = telegramSigner;
        }
        public abstract UniTask<Result<AuthData, string>> Authorize(ConnectionData data);

        protected async UniTask<Result<AuthData, string>> AuthenticateWithCachedData(CachedAuthData? data)
        {
            if (data.HasValue is false)
                throw new ArgumentException($"{nameof(data)} cannot be null.");

            var cachedData = data.Value.CachedData;
            ElympicsLogger.Log($"Starting cached {cachedData.AuthType} authentication...");

            try
            {
                var isJwtTokenExpired = JwtTokenUtil.IsJwtExpired(cachedData.JwtToken);

                if (isJwtTokenExpired is false)
                    return Result<AuthData, string>.Success(cachedData);
                if (data.Value.AutoRetryIfExpired)
                    return await AuthenticateWithAsync(cachedData.AuthType);

                return Result<AuthData, string>.Failure("JWT Token has expired.");
            }
            catch (Exception e)
            {
                throw new ElympicsException("Authentication failed. Reason:" + Environment.NewLine + e.ToString());
            }

        }
        protected async UniTask<Result<AuthData, string>> AuthenticateWithAsync(AuthType authType)
        {
            ElympicsLogger.Log($"Starting {authType} authentication...");
            try
            {
                return authType switch
                {
                    AuthType.ClientSecret => await AuthClient.AuthenticateWithClientSecret(ClientSecret),
                    AuthType.EthAddress => await AuthClient.AuthenticateWithEthAddress(EthSigner),
                    AuthType.Telegram => await AuthClient.AuthenticateWithTelegram(TelegramSigner),
                    AuthType.None => Result<AuthData, string>.Success(null),
                    _ => throw new ArgumentOutOfRangeException(nameof(authType), authType, null)
                };
            }
            catch (Exception e)
            {
                throw new ElympicsException("Authentication failed. Reason:" + Environment.NewLine + e.ToString());
            }
        }
    }
}

