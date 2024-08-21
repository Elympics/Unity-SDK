using System.Collections.Generic;
using UnityEngine;
namespace SCS
{
    [SerializeField]
    public class GetConfigResponse
    {
        public int ChainId;
        public string ChainName;
        public string NativeCurrencyName;
        public string NativeCurrencySymbol;
        public int NativeCurrencyDecimals;
        public string PublicRpcUrl;

        public List<SmartContractDTO> SmartContracts;
    }
}
