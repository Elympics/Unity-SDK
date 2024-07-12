using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
namespace Elympics
{
    internal class AuthorizedStrategy : AuthorizationStrategy
    {
        private readonly AuthData _currentAuthData;

        public AuthorizedStrategy(AuthData currentAuthData, IAuthClient authClient, string clientSecret, ElympicsEthSigner ethSigner, ITelegramSigner telegramSigner) : base(authClient, clientSecret, ethSigner, telegramSigner)
        {
            _currentAuthData = currentAuthData;
        }
        public override async UniTask<Result<AuthData, string>> Authorize(ConnectionData data)
        {
            ElympicsLogger.LogWarning($"Already authorized. Using existing AuthData. To change your authorization data please use {nameof(ElympicsLobbyClient.Instance.SignOut)}");
            return await UniTask.FromResult(Result<AuthData, string>.Success(_currentAuthData));
        }
    }
}
