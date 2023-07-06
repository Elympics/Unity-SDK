using System.Threading;
using System.Threading.Tasks;

namespace Elympics
{
    public interface IEthSigner
    {
        /// <summary>
        /// Method for providing Ethereum public address.
        /// It is called by Elympics in authentication process.
        /// </summary>
        /// <param name="ct">Cancellation token managed by Elympics.</param>
        Task<string> ProvideAddressAsync(CancellationToken ct = default);

        /// <summary>
        /// Method for preparing a message for the player to sign. It MUST contain the nonce passed in the argument.
        /// It is called by Elympics in authentication process.
        /// </summary>
        /// <param name="nonce">A nonce to be included in the message.</param>
        /// <returns>Human-readable message for a player to sign</returns>
        string ProvideMessage(string nonce);

        /// <summary>
        /// Method for signing authentication message from Elympics using "personal_sign" Ethereum method.
        /// It is called by Elympics in authentication process.
        /// </summary>
        /// <param name="message">Hex-encoded UTF-8 message to sign using "personal_sign" algorithm.</param>
        /// <param name="ct">Cancellation token managed by Elympics.</param>
        Task<string> SignAsync(string message, CancellationToken ct = default);
    }
}
