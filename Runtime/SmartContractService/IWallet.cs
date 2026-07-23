#nullable enable

using Cysharp.Threading.Tasks;

namespace SCS
{
    public interface IWallet
    {
        UniTask<string> SignTypedDataV4(string message);
        UniTask<string> SendTransaction(SendTransactionWalletRequest value);
        string Address { get; }
    }
}
