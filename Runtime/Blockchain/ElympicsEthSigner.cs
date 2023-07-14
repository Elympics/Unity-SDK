using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Elympics
{
    public abstract class ElympicsEthSigner : MonoBehaviour, IEthSigner
    {
        public virtual string ProvideMessage(string nonce) =>
            $"Please sign the following nonce so we can authenticate you as Elympics player:\n\n{nonce}";

        public abstract Task<string> ProvideAddressAsync(CancellationToken ct = default);
        public abstract Task<string> SignAsync(string message, CancellationToken ct = default);
    }
}
