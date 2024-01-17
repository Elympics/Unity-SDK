using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elympics.Tests.MockWebClient
{
    internal class EthSignerMock : IEthSigner
    {
        public string TypedData { get; set; }
        public string Signature { get; set; }

        public string Address { get; set; }
        public string ProvideTypedData(string nonce) => TypedData;
        public UniTask<string> SignAsync(string message, CancellationToken ct = default) => UniTask.FromResult(Signature);
    }
}
