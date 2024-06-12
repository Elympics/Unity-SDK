using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IEthSigner
    {
        /// <summary>
        /// Property providing Ethereum public address.
        /// It is retrieved by Elympics in authentication process.
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Method for preparing typed data for the player to sign. It MUST contain the nonce passed in the argument.
        /// It is called by Elympics in authentication process.
        /// </summary>
        /// <param name="nonce">A nonce to be included in the data.</param>
        /// <returns>
        /// Human-readable typed data message (in format defined by <see cref="Blockchain.TypedData.Login"/>)
        /// for a player to sign (serialized to JSON according to EIP-712).
        /// </returns>
        string ProvideTypedData(string nonce);

        /// <summary>
        /// Method for signing authentication message from Elympics using "eth_signTypedData_v4" Ethereum method.
        /// It is called by Elympics in authentication process.
        /// </summary>
        /// <param name="typedData">
        /// Human-readable typed data (in format defined by <see cref="Blockchain.TypedData.Login"/>
        /// and serialized to JSON according to EIP-712) to sign using "eth_signTypedData_v4" algorithm.
        /// </param>
        /// <param name="ct">Cancellation token managed by Elympics.</param>
        UniTask<string> SignAsync(string typedData, CancellationToken ct = default);
    }
}
