using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;

namespace Elympics
{
    internal interface IAuthClient
    {
        UniTask<Result<AuthData, string>> AuthenticateWithClientSecret(string clientSecret, CancellationToken ct = default);
        UniTask<Result<AuthData, string>> AuthenticateWithEthAddress(IEthSigner ethSigner, CancellationToken ct = default);
    }
}
