using UnityEngine;

namespace SCS
{
    [SerializeField]
    public class GetConfigResponse
    {
        public string GameId;
        public int ChainId;
        public string ChainName;
        public string NativeCurrencyName;
        public string NativeCurrencySymbol;
        public int NativeCurrencyDecimals;
        public string PublicRpcUrl;
        public SmartContract[] SmartContracts;

        public override string ToString() => $"{nameof(GameId)}:{GameId}, {nameof(ChainId)}:{ChainId}";
    }
}
