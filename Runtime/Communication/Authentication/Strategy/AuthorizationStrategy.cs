using System;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
namespace Elympics
{
    internal abstract class AuthorizationStrategy
    {
        private readonly IAuthClient _authClient;
        private readonly string _clientSecret;
        private readonly ElympicsEthSigner _ethSigner;
        private readonly ITelegramSigner _telegramSigner;
        internal AuthorizationStrategy(IAuthClient authClient, string clientSecret, ElympicsEthSigner ethSigner, ITelegramSigner telegramSigner)
        {
            _authClient = authClient;
            _clientSecret = clientSecret;
            _ethSigner = ethSigner;
            _telegramSigner = telegramSigner;
        }
        public abstract UniTask<Result<AuthData, string>> Authorize(ConnectionData data);

        protected async UniTask<Result<AuthData, string>> AuthenticateWithCachedData(CachedAuthData? data)
        {
            if (!data.HasValue)
                throw new ArgumentException($"{nameof(data)} cannot be null.");

            var cachedData = data.Value.CachedData;
            ElympicsLogger.Log($"Starting cached {cachedData.AuthType} authentication...");

            try
            {
                var isJwtTokenExpired = JwtTokenUtil.IsJwtExpired(cachedData.JwtToken);

                if (!isJwtTokenExpired)
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
            try
            {
                return authType switch
                {
                    AuthType.ClientSecret => await _authClient.AuthenticateWithClientSecret(_clientSecret),
                    AuthType.EthAddress => await _authClient.AuthenticateWithEthAddress(_ethSigner),
                    AuthType.Telegram => await _authClient.AuthenticateWithTelegram(_telegramSigner),
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

