using System;
using System.Threading;
using Elympics.Models.Authentication;

namespace Elympics
{
	internal interface IAuthClient
	{
		void AuthenticateWithClientSecret(string clientSecret, Action<Result<AuthData, string>> onResult, CancellationToken ct = default);
		void AuthenticateWithEthAddress(IEthSigner ethSigner, Action<Result<AuthData, string>> onResult, CancellationToken ct = default);
	}
}
