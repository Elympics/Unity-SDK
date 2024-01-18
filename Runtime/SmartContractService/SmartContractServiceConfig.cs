using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace SCS
{
    public class SmartContractServiceConfig : ScriptableObject
    {
        public List<ChainConfig> chainConfigs = new();

        public ChainConfig? GetChainConfigForGameId(string gameId) =>
            chainConfigs.FirstOrDefault(x => x.gameConfig.gameId == gameId);
    }
}
