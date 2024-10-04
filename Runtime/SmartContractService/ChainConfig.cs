using System;
using System.Collections.Generic;
using System.Linq;
using Elympics;
#nullable enable

namespace SCS
{
    public readonly struct ChainConfig
    {
        public byte Decimals { get; init; }
        public string ChainId { get; init; }
        public string ChainName { get; init; }
        public string NativeCurrencyName { get; init; }
        public string NativeCurrencySymbol { get; init; }
        public int NativeCurrencyDecimals { get; init; }
        public string PublicRpcUrl { get; init; }
        public List<SmartContract> Contracts { get; init; }

        public SmartContract GetSmartContract(SmartContractType type)
        {
            try
            {
                var smartContract = Contracts.First(x => x.Type == type);
                return smartContract;

            }
            catch (InvalidOperationException e)
            {
                throw new ElympicsException("SmartContractType not found. Please make sure that the specified SmartContractType has been registered on the backend.");
            }
        }
    }
}
