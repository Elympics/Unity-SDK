#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public interface IAuthManager
    {
        event Action<AuthData>? AuthenticationSucceeded;
        event Action<string>? AuthenticationFailed;

        AuthData? AuthData { get; }
        Guid? UserGuid { get; }
        bool IsAuthenticated { get; }

        UniTask ConnectToElympicsAsync(ConnectionData connectionData);

        void SignOut();
    }
}
