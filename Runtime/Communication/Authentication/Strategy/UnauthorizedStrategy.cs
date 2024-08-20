using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;

namespace Elympics
{
    internal class UnauthorizedStrategy : AuthorizationStrategy
    {
        public UnauthorizedStrategy(IAuthClient authClient, string clientSecret, ElympicsEthSigner ethSigner, ITelegramSigner telegramSigner) : base(authClient, clientSecret, ethSigner, telegramSigner)
        { }
        public override async UniTask<Result<AuthData, string>> Authorize(ConnectionData data)
        {
            if (data.AuthFromCacheData.HasValue)
                return await AuthenticateWithCachedData(data.AuthFromCacheData);
            if (data.AuthType.HasValue)
                return await AuthenticateWithAsync(data.AuthType.Value);

            return Result<AuthData, string>.Failure("No data for authentication");
        }

    }
}
