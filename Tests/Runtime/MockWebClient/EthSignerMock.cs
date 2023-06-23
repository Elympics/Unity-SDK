using System.Threading;
using System.Threading.Tasks;

namespace Elympics.Tests.MockWebClient
{
	internal class EthSignerMock : IEthSigner
	{
		public string Address { get; set; }
		public string Message { get; set; }
		public string Signature { get; set; }

		public Task<string> ProvideAddressAsync(CancellationToken ct = default) => Task.FromResult(Address);
		public string ProvideMessage(string nonce) => Message;
		public Task<string> SignAsync(string message, CancellationToken ct = default) => Task.FromResult(Signature);
	}
}
