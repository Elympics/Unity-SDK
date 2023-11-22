using Cysharp.Threading.Tasks;

#nullable enable

namespace SCS
{
    public interface IWallet
    {
        UniTask<string> SignTypedDataV4(string message);
        UniTask<string> SendTransaction(SendTransactionWalletRequest value);
        string Address { get; }
    }
}
