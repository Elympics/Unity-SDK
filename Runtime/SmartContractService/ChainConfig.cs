using System;
using System.Linq;
using Elympics;

#nullable enable

namespace SCS
{
    [Serializable]
    public struct ChainConfig
    {
        public Guid id;
        public string chainId;
        public byte decimals;
        public ElympicsGameConfig gameConfig;
        public SmartContract[] contracts;

        public SmartContract GetSmartContract(SmartContractType type) => contracts.FirstOrDefault(x => x.Type == type.ToString());
    }
}
